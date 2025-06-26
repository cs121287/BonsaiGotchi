using System;
using System.Collections.Generic;
using System.Timers;
using System.Linq;

namespace BonsaiGotchi.EnvironmentSystem
{
    /// <summary>
    /// Manages environmental simulation for the BonsaiGotchi application
    /// </summary>
    public class EnvironmentManager : IDisposable
    {
        #region Properties

        // Current environmental states
        public Season CurrentSeason { get; private set; }
        public Weather CurrentWeather { get; private set; }
        public TimeOfDay CurrentTimeOfDay { get; private set; }
        public ClimateZone CurrentClimate { get; private set; }

        // Game time tracking
        public DateTime GameTime { get; private set; }
        public DateTime InGameTime => GameTime; // Alias for GameTime
        public double TimeMultiplier { get; private set; } = 1.0;

        // Active events
        public List<EnvironmentalEvent> ActiveEvents { get; private set; } = new List<EnvironmentalEvent>();
        public List<EnvironmentalEvent> UpcomingEvents { get; private set; } = new List<EnvironmentalEvent>();

        // Progress percentages
        public double SeasonProgressPercent => ((GameTime.DayOfYear - 1) % SeasonLengthDays) / SeasonLengthDays * 100;
        public double DayProgressPercent => (GameTime.Hour * 60 + GameTime.Minute) / (24.0 * 60) * 100;

        // Environmental conditions
        public double Temperature { get; private set; } = 20.0; // Celsius
        public double Humidity { get; private set; } = 50.0; // Percentage
        public double LightQuality { get; private set; } = 80.0; // Percentage
        public double SoilQuality { get; private set; } = 75.0; // Percentage
        public double AirQuality { get; private set; } = 90.0; // Percentage

        // Weather forecast
        public List<Weather> WeatherForecast { get; private set; } = new List<Weather>();

        // In-game time cycle lengths (real-world minutes)
        private readonly double DayLengthMinutes = 60; // 1 hour = 1 day
        private readonly double SeasonLengthDays = 30; // 1 season = 30 days
        private readonly double YearLengthDays = 120; // 1 year = 120 days (4 seasons)

        // Event generation
        private readonly double EventProbabilityPerDay = 0.15; // 15% chance of event per day

        #endregion

        #region Events

        // Event notifications
        public event EventHandler<SeasonChangedEventArgs> SeasonChanged;
        public event EventHandler<WeatherChangedEventArgs> WeatherChanged;
        public event EventHandler<TimeOfDayChangedEventArgs> TimeOfDayChanged;
        public event EventHandler<EnvironmentalEventArgs> EnvironmentalEventStarted;
        public event EventHandler<EnvironmentalEventArgs> EnvironmentalEventEnded;
        public event EventHandler<YearChangedEventArgs> YearChanged;
        public event EventHandler<ClimateChangedEventArgs> ClimateChanged;

        #endregion

        // Internal state
        private System.Timers.Timer environmentTimer;
        private DateTime lastUpdate;
        private int currentYear = 1;
        private bool disposed = false;
        private Random random;

        /// <summary>
        /// Creates a new environment manager with default settings
        /// </summary>
        public EnvironmentManager(Random randomGenerator = null)
        {
            // Use provided random or create new one
            random = randomGenerator ?? new Random();

            // Initialize with default values
            GameTime = new DateTime(1, 1, 1, 8, 0, 0); // Start at 8 AM on day 1
            CurrentSeason = Season.Spring;
            CurrentTimeOfDay = TimeOfDay.Morning;
            CurrentWeather = GenerateWeatherForSeason(CurrentSeason);
            CurrentClimate = ClimateZone.Temperate;

            // Initialize environmental conditions based on season and weather
            UpdateEnvironmentalConditions();

            // Generate initial weather forecast
            GenerateWeatherForecast();

            // Create timer for environmental updates
            environmentTimer = new System.Timers.Timer(1000); // Update every second
            environmentTimer.Elapsed += OnEnvironmentTimerElapsed;

            lastUpdate = DateTime.Now;
        }

        /// <summary>
        /// Starts the environmental simulation
        /// </summary>
        public void Start()
        {
            lastUpdate = DateTime.Now;
            environmentTimer.Start();
        }

        /// <summary>
        /// Stops the environmental simulation
        /// </summary>
        public void Stop()
        {
            environmentTimer.Stop();
        }

        /// <summary>
        /// Sets the time multiplier
        /// </summary>
        public void SetTimeMultiplier(double multiplier)
        {
            // Clamp to reasonable values
            TimeMultiplier = Math.Max(0.1, Math.Min(100.0, multiplier));
        }

        /// <summary>
        /// Sets the climate zone
        /// </summary>
        public void SetClimateZone(ClimateZone climate)
        {
            if (climate != CurrentClimate)
            {
                ClimateZone oldClimate = CurrentClimate;
                CurrentClimate = climate;

                // Generate appropriate weather for new climate
                CurrentWeather = GenerateWeatherForClimateAndSeason(CurrentClimate, CurrentSeason);

                // Update environmental conditions for new climate
                UpdateEnvironmentalConditions();

                // Update weather forecast
                GenerateWeatherForecast();

                // Notify listeners
                ClimateChanged?.Invoke(this, new ClimateChangedEventArgs(oldClimate, CurrentClimate));
                WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather));
            }
        }

        /// <summary>
        /// Sets the climate zone - alias for SetClimateZone
        /// </summary>
        public void SetClimate(ClimateZone climate)
        {
            SetClimateZone(climate);
        }

        /// <summary>
        /// Main update method called by timer
        /// </summary>
        private void OnEnvironmentTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Calculate elapsed time
            TimeSpan elapsed = DateTime.Now - lastUpdate;
            lastUpdate = DateTime.Now;

            // Apply time multiplier to get game time elapsed
            TimeSpan gameElapsed = TimeSpan.FromMinutes(elapsed.TotalMinutes * TimeMultiplier * (60 / DayLengthMinutes));

            // Update game time
            DateTime oldGameTime = GameTime;
            GameTime = GameTime.Add(gameElapsed);

            // Check for time of day changes
            TimeOfDay oldTimeOfDay = CurrentTimeOfDay;
            UpdateTimeOfDay();

            if (oldTimeOfDay != CurrentTimeOfDay)
            {
                // Notify about time of day change
                TimeOfDayChanged?.Invoke(this, new TimeOfDayChangedEventArgs(oldTimeOfDay, CurrentTimeOfDay));
            }

            // Check for season changes
            Season oldSeason = CurrentSeason;
            UpdateSeason();

            if (oldSeason != CurrentSeason)
            {
                // Notify about season change
                SeasonChanged?.Invoke(this, new SeasonChangedEventArgs(oldSeason, CurrentSeason));

                // Update weather with new season
                Weather oldWeather = CurrentWeather;
                CurrentWeather = GenerateWeatherForClimateAndSeason(CurrentClimate, CurrentSeason);

                if (oldWeather != CurrentWeather)
                {
                    // Notify about weather change due to season
                    WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather));
                }

                // Update weather forecast
                GenerateWeatherForecast();
            }
            else
            {
                // Check for weather changes
                UpdateWeather(gameElapsed);
            }

            // Check for year changes
            if (oldGameTime.Year != GameTime.Year)
            {
                currentYear++;
                YearChanged?.Invoke(this, new YearChangedEventArgs(currentYear));
            }

            // Update environmental events
            UpdateEnvironmentalEvents(gameElapsed);

            // Update environmental conditions
            UpdateEnvironmentalConditions();
        }

        /// <summary>
        /// Updates the current time of day based on game time
        /// </summary>
        private void UpdateTimeOfDay()
        {
            // Determine time of day based on hour
            int hour = GameTime.Hour;

            TimeOfDay newTimeOfDay;

            if (hour >= 5 && hour < 10)
                newTimeOfDay = TimeOfDay.Morning;
            else if (hour >= 10 && hour < 18)
                newTimeOfDay = TimeOfDay.Day;
            else if (hour >= 18 && hour < 22)
                newTimeOfDay = TimeOfDay.Evening;
            else
                newTimeOfDay = TimeOfDay.Night;

            if (CurrentTimeOfDay != newTimeOfDay)
            {
                CurrentTimeOfDay = newTimeOfDay;
            }
        }

        /// <summary>
        /// Updates the current season based on game time
        /// </summary>
        private void UpdateSeason()
        {
            // Calculate day of year (1-120)
            int dayOfYear = ((GameTime.DayOfYear - 1) % (int)YearLengthDays) + 1;

            Season newSeason;

            // Determine season based on day of year
            if (dayOfYear <= (int)SeasonLengthDays)
                newSeason = Season.Spring;
            else if (dayOfYear <= (int)SeasonLengthDays * 2)
                newSeason = Season.Summer;
            else if (dayOfYear <= (int)SeasonLengthDays * 3)
                newSeason = Season.Autumn;
            else
                newSeason = Season.Winter;

            if (CurrentSeason != newSeason)
            {
                CurrentSeason = newSeason;
            }
        }

        /// <summary>
        /// Updates the current weather based on elapsed time
        /// </summary>
        private void UpdateWeather(TimeSpan gameElapsed)
        {
            // Check if it's time to change weather
            // Weather changes happen randomly but more often during season transitions

            // Base probability of 2% per in-game hour, increased near season boundaries
            double hoursPassed = gameElapsed.TotalHours;
            double dayOfSeason = (GameTime.DayOfYear % (int)SeasonLengthDays);
            double seasonTransitionFactor = (dayOfSeason <= 3 || dayOfSeason >= SeasonLengthDays - 3) ? 2.0 : 1.0;
            double weatherChangeChance = 0.02 * hoursPassed * seasonTransitionFactor;

            if (random.NextDouble() < weatherChangeChance)
            {
                Weather oldWeather = CurrentWeather;
                CurrentWeather = GenerateWeatherForClimateAndSeason(CurrentClimate, CurrentSeason);

                if (oldWeather != CurrentWeather)
                {
                    // Notify about weather change
                    WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather));

                    // Update environmental conditions
                    UpdateEnvironmentalConditions();

                    // Update weather forecast
                    GenerateWeatherForecast();
                }
            }
        }

        /// <summary>
        /// Updates active environmental events and may create new ones
        /// </summary>
        private void UpdateEnvironmentalEvents(TimeSpan gameElapsed)
        {
            // Update existing events
            for (int i = ActiveEvents.Count - 1; i >= 0; i--)
            {
                var envEvent = ActiveEvents[i];
                envEvent.RemainingDuration -= gameElapsed.TotalHours;

                if (envEvent.RemainingDuration <= 0)
                {
                    // Event has ended
                    EnvironmentalEventEnded?.Invoke(this, new EnvironmentalEventArgs(envEvent));
                    ActiveEvents.RemoveAt(i);
                }
            }

            // Check for new events
            double dayFraction = gameElapsed.TotalDays;
            double eventChance = EventProbabilityPerDay * dayFraction;

            if (random.NextDouble() < eventChance && ActiveEvents.Count < 2) // Limit concurrent events
            {
                // Create new environmental event
                var newEvent = GenerateRandomEvent();
                ActiveEvents.Add(newEvent);

                // Notify listeners
                EnvironmentalEventStarted?.Invoke(this, new EnvironmentalEventArgs(newEvent));
            }

            // Update upcoming events
            UpdateUpcomingEvents();
        }

        /// <summary>
        /// Update forecast of upcoming environmental events
        /// </summary>
        private void UpdateUpcomingEvents()
        {
            // Clear old events
            UpcomingEvents.Clear();

            // Add potential upcoming events based on season and climate
            int eventCount = random.Next(1, 4);
            for (int i = 0; i < eventCount; i++)
            {
                var futureEvent = GenerateRandomEvent();
                // Set start time to somewhere in the near future (1-5 days)
                futureEvent.StartTime = GameTime.AddHours(random.Next(24, 120));
                // Don't let upcoming events be too short
                futureEvent.Duration = Math.Max(futureEvent.Duration, 8);
                UpcomingEvents.Add(futureEvent);
            }
        }

        /// <summary>
        /// Updates environmental conditions based on season, weather, and time of day
        /// </summary>
        private void UpdateEnvironmentalConditions()
        {
            // Calculate base temperature based on season and climate
            double baseTemp = CurrentSeason switch
            {
                Season.Spring => 15,
                Season.Summer => 25,
                Season.Autumn => 15,
                Season.Winter => 0,
                _ => 15
            };

            // Adjust for climate
            baseTemp += CurrentClimate switch
            {
                ClimateZone.Tropical => 10,
                ClimateZone.Desert => 15,
                ClimateZone.Alpine => -10,
                _ => 0
            };

            // Adjust for time of day
            double timeOfDayMod = CurrentTimeOfDay switch
            {
                TimeOfDay.Morning => -2,
                TimeOfDay.Day => 5,
                TimeOfDay.Evening => 0,
                TimeOfDay.Night => -5,
                _ => 0
            };
            baseTemp += timeOfDayMod;

            // Adjust for weather
            baseTemp += CurrentWeather switch
            {
                Weather.Sunny => 5,
                Weather.Cloudy => 0,
                Weather.Rain => -3,
                Weather.Humid => 2,
                Weather.Wind => -2,
                Weather.Storm => -5,
                Weather.Snow => -10,
                _ => 0
            };

            // Add random variation
            baseTemp += random.NextDouble() * 2 - 1;

            // Set temperature
            Temperature = baseTemp;

            // Calculate humidity based on weather and climate
            double baseHumidity = CurrentWeather switch
            {
                Weather.Rain => 90,
                Weather.Snow => 85,
                Weather.Humid => 95,
                Weather.Storm => 80,
                Weather.Cloudy => 65,
                Weather.Sunny => 40,
                Weather.Wind => 30,
                _ => 50
            };

            // Adjust for climate
            baseHumidity += CurrentClimate switch
            {
                ClimateZone.Tropical => 20,
                ClimateZone.Desert => -20,
                ClimateZone.Alpine => 10,
                _ => 0
            };

            // Add random variation
            baseHumidity += random.NextDouble() * 10 - 5;

            // Clamp to valid range
            Humidity = Math.Max(5, Math.Min(100, baseHumidity));

            // Calculate light quality based on weather and time of day
            double baseLight = CurrentTimeOfDay switch
            {
                TimeOfDay.Day => 100,
                TimeOfDay.Morning => 80,
                TimeOfDay.Evening => 60,
                TimeOfDay.Night => 10,
                _ => 50
            };

            // Adjust for weather
            baseLight *= CurrentWeather switch
            {
                Weather.Sunny => 1.0,
                Weather.Cloudy => 0.6,
                Weather.Rain => 0.4,
                Weather.Humid => 0.7,
                Weather.Wind => 0.8,
                Weather.Storm => 0.3,
                Weather.Snow => 0.9, // Snow can be bright
                _ => 0.7
            };

            // Clamp to valid range
            LightQuality = Math.Max(5, Math.Min(100, baseLight));

            // Calculate soil quality based on season and weather
            double baseSoil = 75; // Start with decent soil

            // Adjust for weather trends
            if (CurrentWeather == Weather.Rain || CurrentWeather == Weather.Humid)
            {
                // Too much water decreases soil quality
                baseSoil -= 5;
            }
            else if (CurrentWeather == Weather.Sunny && CurrentSeason == Season.Summer)
            {
                // Hot and dry decreases soil quality
                baseSoil -= 10;
            }

            // Add random variation
            baseSoil += random.NextDouble() * 6 - 3;

            // Clamp to valid range
            SoilQuality = Math.Max(20, Math.Min(100, baseSoil));

            // Calculate air quality based on weather and climate
            double baseAir = 85; // Start with good air

            // Adjust for weather
            baseAir += CurrentWeather switch
            {
                Weather.Rain => 10, // Rain cleans air
                Weather.Wind => 5,  // Wind can clean air
                Weather.Storm => 0, // Neutral
                Weather.Humid => -5, // Can trap pollutants
                _ => 0
            };

            // Add random variation
            baseAir += random.NextDouble() * 6 - 3;

            // Clamp to valid range
            AirQuality = Math.Max(20, Math.Min(100, baseAir));
        }

        /// <summary>
        /// Generates a weather forecast for the next few days
        /// </summary>
        private void GenerateWeatherForecast()
        {
            // Clear old forecast
            WeatherForecast.Clear();

            // Generate forecast for next 5 days
            for (int i = 0; i < 5; i++)
            {
                // Calculate season for the forecast day
                int dayOffset = i + 1;
                DateTime forecastDate = GameTime.AddDays(dayOffset);
                int dayOfYear = ((forecastDate.DayOfYear - 1) % (int)YearLengthDays) + 1;

                Season forecastSeason;
                if (dayOfYear <= (int)SeasonLengthDays)
                    forecastSeason = Season.Spring;
                else if (dayOfYear <= (int)SeasonLengthDays * 2)
                    forecastSeason = Season.Summer;
                else if (dayOfYear <= (int)SeasonLengthDays * 3)
                    forecastSeason = Season.Autumn;
                else
                    forecastSeason = Season.Winter;

                // Generate weather for that day (accuracy decreases with distance)
                Weather forecastWeather;
                double accuracyFactor = 1.0 - (i * 0.2); // Starts at 1.0, decreases by 0.2 each day

                if (random.NextDouble() > accuracyFactor)
                {
                    // Inaccurate forecast
                    forecastWeather = GenerateWeatherForClimateAndSeason(CurrentClimate, forecastSeason);
                }
                else
                {
                    // Use actual weather prediction logic
                    forecastWeather = GenerateWeatherForClimateAndSeason(CurrentClimate, forecastSeason);
                }

                WeatherForecast.Add(forecastWeather);
            }
        }

        /// <summary>
        /// Generates weather appropriate for the current season
        /// </summary>
        private Weather GenerateWeatherForSeason(Season season)
        {
            return GenerateWeatherForClimateAndSeason(CurrentClimate, season);
        }

        /// <summary>
        /// Generates weather appropriate for the given climate and season
        /// </summary>
        private Weather GenerateWeatherForClimateAndSeason(ClimateZone climate, Season season)
        {
            double roll = random.NextDouble();

            // Different weather probabilities based on climate and season
            switch (climate)
            {
                case ClimateZone.Temperate:
                    return season switch
                    {
                        Season.Spring => roll switch
                        {
                            < 0.4 => Weather.Cloudy,
                            < 0.7 => Weather.Rain,
                            < 0.9 => Weather.Sunny,
                            _ => Weather.Wind
                        },
                        Season.Summer => roll switch
                        {
                            < 0.6 => Weather.Sunny,
                            < 0.8 => Weather.Cloudy,
                            < 0.9 => Weather.Rain,
                            _ => Weather.Humid
                        },
                        Season.Autumn => roll switch
                        {
                            < 0.4 => Weather.Wind,
                            < 0.7 => Weather.Cloudy,
                            < 0.9 => Weather.Rain,
                            _ => Weather.Sunny
                        },
                        Season.Winter => roll switch
                        {
                            < 0.4 => Weather.Cloudy,
                            < 0.7 => Weather.Snow,
                            < 0.9 => Weather.Wind,
                            _ => Weather.Rain
                        },
                        _ => Weather.Cloudy
                    };

                case ClimateZone.Tropical:
                    return season switch
                    {
                        Season.Spring => roll switch
                        {
                            < 0.4 => Weather.Rain,
                            < 0.7 => Weather.Humid,
                            < 0.9 => Weather.Sunny,
                            _ => Weather.Cloudy
                        },
                        Season.Summer => roll switch
                        {
                            < 0.5 => Weather.Humid,
                            < 0.8 => Weather.Sunny,
                            < 0.9 => Weather.Storm,
                            _ => Weather.Rain
                        },
                        Season.Autumn => roll switch
                        {
                            < 0.5 => Weather.Rain,
                            < 0.8 => Weather.Humid,
                            < 0.9 => Weather.Storm,
                            _ => Weather.Cloudy
                        },
                        Season.Winter => roll switch
                        {
                            < 0.5 => Weather.Rain,
                            < 0.8 => Weather.Cloudy,
                            < 0.9 => Weather.Humid,
                            _ => Weather.Sunny
                        },
                        _ => Weather.Humid
                    };

                case ClimateZone.Desert:
                    return season switch
                    {
                        Season.Spring => roll switch
                        {
                            < 0.7 => Weather.Sunny,
                            < 0.9 => Weather.Wind,
                            _ => Weather.Cloudy
                        },
                        Season.Summer => roll switch
                        {
                            < 0.8 => Weather.Sunny,
                            < 0.95 => Weather.Wind,
                            _ => Weather.Storm
                        },
                        Season.Autumn => roll switch
                        {
                            < 0.6 => Weather.Sunny,
                            < 0.9 => Weather.Wind,
                            _ => Weather.Rain // Rare rain
                        },
                        Season.Winter => roll switch
                        {
                            < 0.5 => Weather.Sunny,
                            < 0.8 => Weather.Cloudy,
                            < 0.95 => Weather.Wind,
                            _ => Weather.Rain // Very rare rain
                        },
                        _ => Weather.Sunny
                    };

                case ClimateZone.Alpine:
                    return season switch
                    {
                        Season.Spring => roll switch
                        {
                            < 0.4 => Weather.Snow,
                            < 0.7 => Weather.Rain,
                            < 0.9 => Weather.Cloudy,
                            _ => Weather.Sunny
                        },
                        Season.Summer => roll switch
                        {
                            < 0.4 => Weather.Sunny,
                            < 0.7 => Weather.Cloudy,
                            < 0.9 => Weather.Rain,
                            _ => Weather.Wind
                        },
                        Season.Autumn => roll switch
                        {
                            < 0.3 => Weather.Rain,
                            < 0.6 => Weather.Wind,
                            < 0.8 => Weather.Cloudy,
                            < 0.95 => Weather.Snow,
                            _ => Weather.Sunny
                        },
                        Season.Winter => roll switch
                        {
                            < 0.6 => Weather.Snow,
                            < 0.8 => Weather.Cloudy,
                            < 0.9 => Weather.Wind,
                            _ => Weather.Sunny
                        },
                        _ => Weather.Snow
                    };

                default:
                    return Weather.Cloudy; // Default if unknown climate
            }
        }

        /// <summary>
        /// Creates a random environmental event appropriate for the current season and climate
        /// </summary>
        private EnvironmentalEvent GenerateRandomEvent()
        {
            // Different event probabilities based on season and climate
            List<EventType> possibleEvents = new List<EventType>();

            // Add possible events based on season
            switch (CurrentSeason)
            {
                case Season.Spring:
                    possibleEvents.Add(EventType.HeavyRain);
                    possibleEvents.Add(EventType.Insects);
                    possibleEvents.Add(EventType.Heatwave); // Rare
                    possibleEvents.Add(EventType.SunnySpell);
                    break;

                case Season.Summer:
                    possibleEvents.Add(EventType.Heatwave);
                    possibleEvents.Add(EventType.Drought);
                    possibleEvents.Add(EventType.Insects);
                    possibleEvents.Add(EventType.Storm);
                    possibleEvents.Add(EventType.SunnySpell);
                    break;

                case Season.Autumn:
                    possibleEvents.Add(EventType.HeavyRain);
                    possibleEvents.Add(EventType.Storm);
                    possibleEvents.Add(EventType.Insects);
                    possibleEvents.Add(EventType.SunnySpell);
                    possibleEvents.Add(EventType.WindySpell);
                    break;

                case Season.Winter:
                    possibleEvents.Add(EventType.ColdSnap);
                    possibleEvents.Add(EventType.Frost);
                    possibleEvents.Add(EventType.BlizzardWarning);
                    possibleEvents.Add(EventType.SunnySpell);
                    break;
            }

            // Modify probabilities based on climate
            List<EventType> climateSuitableEvents = new List<EventType>();

            foreach (var eventType in possibleEvents)
            {
                bool includeEvent = true;

                // Filter out climate-incompatible events
                switch (CurrentClimate)
                {
                    case ClimateZone.Desert:
                        if (eventType == EventType.Frost || eventType == EventType.ColdSnap ||
                            eventType == EventType.HeavyRain || eventType == EventType.BlizzardWarning)
                        {
                            includeEvent = false;
                        }
                        break;

                    case ClimateZone.Tropical:
                        if (eventType == EventType.Frost || eventType == EventType.ColdSnap ||
                            eventType == EventType.BlizzardWarning)
                        {
                            includeEvent = false;
                        }
                        break;

                    case ClimateZone.Alpine:
                        if (eventType == EventType.Drought || eventType == EventType.Heatwave)
                        {
                            includeEvent = false;
                        }
                        break;
                }

                if (includeEvent)
                {
                    // Add multiple entries to increase probability of common events for this climate
                    switch (CurrentClimate)
                    {
                        case ClimateZone.Desert:
                            if (eventType == EventType.Drought || eventType == EventType.Heatwave)
                            {
                                climateSuitableEvents.Add(eventType);
                                climateSuitableEvents.Add(eventType); // Add twice for higher probability
                            }
                            else
                            {
                                climateSuitableEvents.Add(eventType);
                            }
                            break;

                        case ClimateZone.Tropical:
                            if (eventType == EventType.HeavyRain || eventType == EventType.Storm ||
                                eventType == EventType.Insects)
                            {
                                climateSuitableEvents.Add(eventType);
                                climateSuitableEvents.Add(eventType); // Add twice for higher probability
                            }
                            else
                            {
                                climateSuitableEvents.Add(eventType);
                            }
                            break;

                        case ClimateZone.Alpine:
                            if (eventType == EventType.Frost || eventType == EventType.ColdSnap ||
                                eventType == EventType.BlizzardWarning)
                            {
                                climateSuitableEvents.Add(eventType);
                                climateSuitableEvents.Add(eventType); // Add twice for higher probability
                            }
                            else
                            {
                                climateSuitableEvents.Add(eventType);
                            }
                            break;

                        default:
                            climateSuitableEvents.Add(eventType);
                            break;
                    }
                }
            }

            // If we filtered too aggressively and have no events, add default events
            if (climateSuitableEvents.Count == 0)
            {
                climateSuitableEvents.Add(EventType.SunnySpell);
                climateSuitableEvents.Add(EventType.WindySpell);
            }

            // Select a random event from the suitable ones
            EventType selectedEvent = climateSuitableEvents[random.Next(climateSuitableEvents.Count)];

            // Generate intensity and duration
            int intensity = random.Next(30, 100);
            double duration = random.Next(4, 48); // 4 to 48 hours

            return new EnvironmentalEvent(selectedEvent, intensity, duration);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    environmentTimer?.Stop();
                    environmentTimer?.Dispose();
                }

                disposed = true;
            }
        }
    }

    #region Support Classes

    /// <summary>
    /// Represents an environmental event
    /// </summary>
    public class EnvironmentalEvent
    {
        public EventType Type { get; set; }
        public int Intensity { get; set; } // 1-100
        public double RemainingDuration { get; set; } // Hours
        public double Duration { get; set; } // Total hours
        public DateTime StartTime { get; set; } // When the event starts or started

        public EnvironmentalEvent(EventType type, int intensity, double duration)
        {
            Type = type;
            Intensity = intensity;
            Duration = duration;
            RemainingDuration = duration;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Gets a description of the event based on type and intensity
        /// </summary>
        public string GetDescription()
        {
            string intensityDesc = Intensity > 70 ? "severe" : (Intensity > 40 ? "moderate" : "mild");

            return Type switch
            {
                EventType.Heatwave => $"A {intensityDesc} heatwave",
                EventType.Drought => $"A {intensityDesc} drought",
                EventType.HeavyRain => $"{intensityDesc} heavy rainfall",
                EventType.Storm => $"A {intensityDesc} storm",
                EventType.ColdSnap => $"A {intensityDesc} cold snap",
                EventType.Frost => $"{intensityDesc} frost",
                EventType.Insects => $"A {intensityDesc} insect infestation",
                EventType.BlizzardWarning => $"A {intensityDesc} blizzard warning",
                EventType.SunnySpell => $"A {intensityDesc} sunny spell",
                EventType.WindySpell => $"A {intensityDesc} windy spell",
                _ => $"Unknown environmental event ({intensityDesc})"
            };
        }
    }

    #endregion

    #region Event Args

    /// <summary>
    /// Event arguments for season changes
    /// </summary>
    public class SeasonChangedEventArgs : EventArgs
    {
        public Season OldSeason { get; }
        public Season NewSeason { get; }
        public Season Season => NewSeason; // Alias for NewSeason

        public SeasonChangedEventArgs(Season oldSeason, Season newSeason)
        {
            OldSeason = oldSeason;
            NewSeason = newSeason;
        }
    }

    /// <summary>
    /// Event arguments for weather changes
    /// </summary>
    public class WeatherChangedEventArgs : EventArgs
    {
        public Weather NewWeather { get; }

        public WeatherChangedEventArgs(Weather newWeather)
        {
            NewWeather = newWeather;
        }
    }

    /// <summary>
    /// Event arguments for time of day changes
    /// </summary>
    public class TimeOfDayChangedEventArgs : EventArgs
    {
        public TimeOfDay OldTimeOfDay { get; }
        public TimeOfDay NewTimeOfDay { get; }

        public TimeOfDayChangedEventArgs(TimeOfDay oldTimeOfDay, TimeOfDay newTimeOfDay)
        {
            OldTimeOfDay = oldTimeOfDay;
            NewTimeOfDay = newTimeOfDay;
        }
    }

    /// <summary>
    /// Event arguments for environmental events
    /// </summary>
    public class EnvironmentalEventArgs : EventArgs
    {
        public EnvironmentalEvent Event { get; }

        public EnvironmentalEventArgs(EnvironmentalEvent envEvent)
        {
            Event = envEvent;
        }
    }

    /// <summary>
    /// Event arguments for year changes
    /// </summary>
    public class YearChangedEventArgs : EventArgs
    {
        public int Year { get; }

        public YearChangedEventArgs(int year)
        {
            Year = year;
        }
    }

    /// <summary>
    /// Event arguments for climate changes
    /// </summary>
    public class ClimateChangedEventArgs : EventArgs
    {
        public ClimateZone OldClimate { get; }
        public ClimateZone NewClimate { get; }

        public ClimateChangedEventArgs(ClimateZone oldClimate, ClimateZone newClimate)
        {
            OldClimate = oldClimate;
            NewClimate = newClimate;
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Time of day
    /// </summary>
    public enum TimeOfDay
    {
        Morning,
        Day,
        Evening,
        Night
    }

    /// <summary>
    /// Climate zones
    /// </summary>
    public enum ClimateZone
    {
        Temperate,
        Tropical,
        Desert,
        Alpine
    }

    /// <summary>
    /// Types of environmental events
    /// </summary>
    public enum EventType
    {
        Heatwave,
        Drought,
        HeavyRain,
        Storm,
        ColdSnap,
        Frost,
        Insects,
        BlizzardWarning,
        SunnySpell,
        WindySpell
    }

    #endregion
}