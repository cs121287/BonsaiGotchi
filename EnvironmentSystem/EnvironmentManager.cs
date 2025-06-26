using System;
using System.Collections.Generic;
using System.Timers;

namespace BonsaiGotchi.EnvironmentSystem
{
    /// <summary>
    /// Manages environmental factors that affect bonsai growth
    /// </summary>
    public class EnvironmentManager
    {
        #region Environmental Properties
        
        // Current climate settings
        public ClimateZone CurrentClimate { get; private set; } = ClimateZone.Temperate;
        
        // Season cycle
        public Season CurrentSeason { get; private set; }
        public DateTime LastSeasonChange { get; private set; }
        public int SeasonLengthDays { get; set; } = 90; // Real days per season
        public double SeasonProgressPercent { get; private set; }
        
        // Weather system
        public Weather CurrentWeather { get; private set; }
        public DateTime LastWeatherChange { get; private set; }
        public int WeatherDurationHours { get; private set; } = 24; // Hours each weather lasts
        public List<WeatherForecast> WeatherForecast { get; private set; } = new List<WeatherForecast>();
        
        // Day/night cycle
        public TimeOfDay CurrentTimeOfDay { get; private set; }
        public DateTime LastDayCycleChange { get; private set; }
        public double DayProgressPercent { get; private set; }
        public DateTime InGameTime { get; private set; }
        public double TimeMultiplier { get; private set; } = 1.0; // Default: 1 game hour = 1 real minute
        
        // Special events
        public List<EnvironmentalEvent> ActiveEvents { get; private set; } = new List<EnvironmentalEvent>();
        public List<EnvironmentalEvent> UpcomingEvents { get; private set; } = new List<EnvironmentalEvent>();
        
        // Environmental quality
        public double AirQuality { get; private set; } = 100;
        public double SoilQuality { get; private set; } = 100;
        public double LightQuality { get; private set; } = 100;
        public double Humidity { get; private set; } = 50;
        public double Temperature { get; private set; } = 72; // Fahrenheit
        
        #endregion
        
        #region Events
        
        // Events for UI updates and notifications
        public event EventHandler<SeasonChangedEventArgs> SeasonChanged;
        public event EventHandler<WeatherChangedEventArgs> WeatherChanged;
        public event EventHandler<TimeOfDayChangedEventArgs> TimeOfDayChanged;
        public event EventHandler<EnvironmentalEventArgs> EnvironmentalEventStarted;
        public event EventHandler<EnvironmentalEventArgs> EnvironmentalEventEnded;
        
        #endregion
        
        // Timer for environment updates
        private System.Timers.Timer environmentTimer;
        private readonly Random random;
        
        /// <summary>
        /// Initialize the environment manager with default settings
        /// </summary>
        public EnvironmentManager(Random random)
        {
            this.random = random;
            
            // Initialize environmental factors based on current date
            InitializeEnvironment();
            
            // Create timer for environmental updates
            environmentTimer = new System.Timers.Timer(60000); // Update every minute
            environmentTimer.Elapsed += OnEnvironmentTimerElapsed;
        }
        
        /// <summary>
        /// Start environmental simulation
        /// </summary>
        public void Start()
        {
            environmentTimer.Start();
        }
        
        /// <summary>
        /// Stop environmental simulation
        /// </summary>
        public void Stop()
        {
            environmentTimer.Stop();
        }
        
        #region Environment Initialization
        
        /// <summary>
        /// Initialize environmental factors
        /// </summary>
        private void InitializeEnvironment()
        {
            DateTime now = DateTime.Now;
            
            // Initialize season based on current date
            CurrentSeason = GetSeasonForMonth(now.Month);
            LastSeasonChange = now;
            
            // Initialize weather based on season
            CurrentWeather = GetRandomWeatherForSeason(CurrentSeason, CurrentClimate);
            LastWeatherChange = now;
            
            // Initialize time of day
            CurrentTimeOfDay = GetTimeOfDayForHour(now.Hour);
            LastDayCycleChange = now;
            
            // Initialize in-game time
            InGameTime = new DateTime(1, 1, 1, now.Hour, now.Minute, 0); // Start at current time on day 1
            
            // Generate weather forecast
            GenerateWeatherForecast();
        }
        
        /// <summary>
        /// Determine season based on month
        /// </summary>
        private Season GetSeasonForMonth(int month)
        {
            return month switch
            {
                12 or 1 or 2 => Season.Winter,
                3 or 4 or 5 => Season.Spring,
                6 or 7 or 8 => Season.Summer,
                _ => Season.Autumn
            };
        }
        
        /// <summary>
        /// Get time of day based on hour
        /// </summary>
        private TimeOfDay GetTimeOfDayForHour(int hour)
        {
            return hour switch
            {
                >= 6 and < 10 => TimeOfDay.Morning,
                >= 10 and < 16 => TimeOfDay.Day,
                >= 16 and < 20 => TimeOfDay.Evening,
                _ => TimeOfDay.Night
            };
        }
        
        /// <summary>
        /// Generate a random weather condition appropriate for the season and climate
        /// </summary>
        private Weather GetRandomWeatherForSeason(Season season, ClimateZone climate)
        {
            double roll = random.NextDouble();
            
            // Different weather probabilities based on season and climate
            return (season, climate) switch
            {
                // Temperate climate
                (Season.Spring, ClimateZone.Temperate) => roll switch
                {
                    < 0.3 => Weather.Rain,
                    < 0.6 => Weather.Cloudy,
                    < 0.8 => Weather.Sunny,
                    < 0.95 => Weather.Wind,
                    _ => Weather.Storm
                },
                (Season.Summer, ClimateZone.Temperate) => roll switch
                {
                    < 0.5 => Weather.Sunny,
                    < 0.7 => Weather.Cloudy,
                    < 0.85 => Weather.Rain,
                    < 0.95 => Weather.Humid,
                    _ => Weather.Storm
                },
                (Season.Autumn, ClimateZone.Temperate) => roll switch
                {
                    < 0.3 => Weather.Wind,
                    < 0.6 => Weather.Cloudy,
                    < 0.8 => Weather.Rain,
                    < 0.95 => Weather.Sunny,
                    _ => Weather.Storm
                },
                (Season.Winter, ClimateZone.Temperate) => roll switch
                {
                    < 0.4 => Weather.Cloudy,
                    < 0.6 => Weather.Snow,
                    < 0.8 => Weather.Sunny, // Cold sunny day
                    < 0.95 => Weather.Wind,
                    _ => Weather.Storm
                },
                
                // Tropical climate
                (_, ClimateZone.Tropical) => roll switch
                {
                    < 0.3 => Weather.Humid,
                    < 0.5 => Weather.Rain,
                    < 0.8 => Weather.Sunny,
                    < 0.95 => Weather.Cloudy,
                    _ => Weather.Storm
                },
                
                // Desert climate
                (_, ClimateZone.Desert) => roll switch
                {
                    < 0.7 => Weather.Sunny,
                    < 0.9 => Weather.Wind,
                    < 0.95 => Weather.Cloudy,
                    _ => Weather.Rain // Rare rain
                },
                
                // Alpine climate
                (Season.Winter, ClimateZone.Alpine) => roll switch
                {
                    < 0.5 => Weather.Snow,
                    < 0.8 => Weather.Cloudy,
                    < 0.95 => Weather.Wind,
                    _ => Weather.Storm
                },
                (_, ClimateZone.Alpine) => roll switch
                {
                    < 0.3 => Weather.Cloudy,
                    < 0.5 => Weather.Rain,
                    < 0.7 => Weather.Sunny,
                    < 0.9 => Weather.Wind,
                    _ => Weather.Storm
                },
                
                // Default
                _ => Weather.Cloudy
            };
        }
        
        /// <summary>
        /// Generate a 3-day weather forecast
        /// </summary>
        private void GenerateWeatherForecast()
        {
            WeatherForecast.Clear();
            
            // Current weather is first in forecast
            WeatherForecast.Add(new WeatherForecast
                Weather = CurrentWeather,
                Day = 0,
                Temperature = Temperature,
                Probability = 100 // Current weather is certain
            });
            
            // Generate forecasts for next 3 days
            for (int i = 1; i <= 3; i++)
            {
                // Weather tends to follow patterns but can change
                Weather prevWeather = i == 1 ? CurrentWeather : WeatherForecast[i - 1].Weather;
                Weather nextWeather = GetNextDayWeather(prevWeather, CurrentSeason, CurrentClimate);
                
                // Calculate likely temperature
                double nextTemp = PredictTemperatureForWeather(nextWeather, CurrentSeason, CurrentClimate);
                
                // Forecast accuracy decreases with time
                int probability = 100 - (i * 20); // 80%, 60%, 40%
                
                WeatherForecast.Add(new WeatherForecast
                {
                    Weather = nextWeather,
                    Day = i,
                    Temperature = nextTemp,
                    Probability = probability
                });
            }
        }
        
        /// <summary>
        /// Predict next day's weather based on current weather, season, and climate
        /// </summary>
        private Weather GetNextDayWeather(Weather currentWeather, Season season, ClimateZone climate)
        {
            // 60% chance to continue similar weather pattern, 40% chance to change
            if (random.NextDouble() < 0.6)
            {
                // Similar weather with minor changes
                return currentWeather switch
                {
                    Weather.Sunny => random.NextDouble() < 0.8 ? Weather.Sunny : Weather.Cloudy,
                    Weather.Cloudy => random.NextDouble() < 0.4 ? Weather.Cloudy :
                                     random.NextDouble() < 0.7 ? Weather.Sunny : Weather.Rain,
                    Weather.Rain => random.NextDouble() < 0.5 ? Weather.Rain : Weather.Cloudy,
                    Weather.Humid => random.NextDouble() < 0.6 ? Weather.Humid :
                                    random.NextDouble() < 0.8 ? Weather.Rain : Weather.Sunny,
                    Weather.Snow => season == Season.Winter ? 
                                   (random.NextDouble() < 0.7 ? Weather.Snow : Weather.Cloudy) : 
                                   Weather.Cloudy,
                    Weather.Wind => random.NextDouble() < 0.4 ? Weather.Wind : Weather.Cloudy,
                    Weather.Storm => random.NextDouble() < 0.3 ? Weather.Storm : 
                                    random.NextDouble() < 0.7 ? Weather.Rain : Weather.Cloudy,
                    _ => Weather.Cloudy
                };
            }
            else
            {
                // New weather pattern
                return GetRandomWeatherForSeason(season, climate);
            }
        }
        
        /// <summary>
        /// Predict temperature based on weather, season, and climate
        /// </summary>
        private double PredictTemperatureForWeather(Weather weather, Season season, ClimateZone climate)
        {
            // Base temperature by climate zone
            double baseTemp = climate switch
            {
                ClimateZone.Desert => 85,
                ClimateZone.Tropical => 82,
                ClimateZone.Temperate => 72,
                ClimateZone.Alpine => 55,
                _ => 72
            };
            
            // Seasonal adjustment
            double seasonAdjust = season switch
            {
                Season.Winter => -20,
                Season.Spring => -5,
                Season.Summer => +10,
                Season.Autumn => -10,
                _ => 0
            };
            
            // Weather adjustment
            double weatherAdjust = weather switch
            {
                Weather.Sunny => +5,
                Weather.Snow => -15,
                Weather.Rain => -8,
                Weather.Storm => -12,
                Weather.Wind => -5,
                Weather.Cloudy => -3,
                Weather.Humid => +2,
                _ => 0
            };
            
            // Random variation (+/- 3 degrees)
            double randomVariation = random.NextDouble() * 6 - 3;
            
            return baseTemp + seasonAdjust + weatherAdjust + randomVariation;
        }
        
        #endregion
        
        #region Environment Updates
        
        /// <summary>
        /// Timer event handler for environment updates
        /// </summary>
        private void OnEnvironmentTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateEnvironment();
        }
        
        /// <summary>
        /// Update all environmental factors
        /// </summary>
        public void UpdateEnvironment()
        {
            DateTime now = DateTime.Now;
            
            // Update in-game time
            UpdateGameTime();
            
            // Check for season change
            CheckForSeasonChange(now);
            
            // Check for weather change
            CheckForWeatherChange(now);
            
            // Check for time of day change
            CheckForTimeOfDayChange(now);
            
            // Update environmental conditions
            UpdateEnvironmentalConditions();
            
            // Check for environmental events
            CheckForEnvironmentalEvents();
        }
        
        /// <summary>
        /// Update the in-game time based on time multiplier
        /// </summary>
        private void UpdateGameTime()
        {
            // Calculate elapsed minutes since last update
            TimeSpan elapsed = DateTime.Now - LastDayCycleChange;
            double gameMinutesElapsed = elapsed.TotalMinutes * 60 * TimeMultiplier;
            
            // Update in-game time
            InGameTime = InGameTime.AddMinutes(gameMinutesElapsed);
            
            // Update last update time
            LastDayCycleChange = DateTime.Now;
            
            // Calculate day progress percentage (0-100)
            int minutesInDay = 24 * 60;
            int currentMinutes = InGameTime.Hour * 60 + InGameTime.Minute;
            DayProgressPercent = (currentMinutes * 100.0) / minutesInDay;
        }
        
        /// <summary>
        /// Check if season should change
        /// </summary>
        private void CheckForSeasonChange(DateTime now)
        {
            // Calculate days since last season change
            double daysSinceChange = (now - LastSeasonChange).TotalDays;
            
            // Update season progress percentage
            SeasonProgressPercent = Math.Min(100, (daysSinceChange * 100.0) / SeasonLengthDays);
            
            // Check if it's time for a season change
            if (daysSinceChange >= SeasonLengthDays)
            {
                // Move to next season
                CurrentSeason = CurrentSeason switch
                {
                    Season.Winter => Season.Spring,
                    Season.Spring => Season.Summer,
                    Season.Summer => Season.Autumn,
                    Season.Autumn => Season.Winter,
                    _ => Season.Spring
                };
                
                // Reset last change time
                LastSeasonChange = now;
                
                // Reset progress
                SeasonProgressPercent = 0;
                
                // Trigger season changed event
                SeasonChanged?.Invoke(this, new SeasonChangedEventArgs(CurrentSeason));
                
                // Generate special seasonal events
                GenerateSeasonalEvents();
                
                // Update weather for new season
                CurrentWeather = GetRandomWeatherForSeason(CurrentSeason, CurrentClimate);
                LastWeatherChange = now;
                
                // Update weather forecast for the new season
                GenerateWeatherForecast();
                
                // Trigger weather changed event
                WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather, Temperature));
            }
        }
        
        /// <summary>
        /// Check if weather should change
        /// </summary>
        private void CheckForWeatherChange(DateTime now)
        {
            // Convert real hours to game hours based on multiplier
            double hoursSinceChange = (now - LastWeatherChange).TotalHours;
            double gameHoursSinceChange = hoursSinceChange * TimeMultiplier;
            
            // Check if it's time for a weather change
            if (gameHoursSinceChange >= WeatherDurationHours)
            {
                // Get new weather
                Weather newWeather = GetNextDayWeather(CurrentWeather, CurrentSeason, CurrentClimate);
                
                // Only update if actually changed
                if (newWeather != CurrentWeather)
                {
                    // Update weather
                    CurrentWeather = newWeather;
                    
                    // Update temperature based on new weather
                    Temperature = PredictTemperatureForWeather(CurrentWeather, CurrentSeason, CurrentClimate);
                    
                    // Reset last change time
                    LastWeatherChange = now;
                    
                    // Update humidity based on weather
                    UpdateHumidityForWeather();
                    
                    // Update weather forecast
                    GenerateWeatherForecast();
                    
                    // Trigger weather changed event
                    WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather, Temperature));
                }
                else
                {
                    // Weather stays the same, just reset timer
                    LastWeatherChange = now;
                }
            }
        }
        
        /// <summary>
        /// Check if time of day should change
        /// </summary>
        private void CheckForTimeOfDayChange(DateTime now)
        {
            // Calculate current in-game hour
            int gameHour = InGameTime.Hour;
            
            // Determine time of day
            TimeOfDay newTimeOfDay = GetTimeOfDayForHour(gameHour);
            
            // Check if time of day has changed
            if (newTimeOfDay != CurrentTimeOfDay)
            {
                TimeOfDay previousTimeOfDay = CurrentTimeOfDay;
                CurrentTimeOfDay = newTimeOfDay;
                
                // Trigger time of day changed event
                TimeOfDayChanged?.Invoke(this, new TimeOfDayChangedEventArgs(
                    CurrentTimeOfDay, previousTimeOfDay, gameHour));
                
                // Update environmental factors based on time of day
                UpdateForTimeOfDay();
            }
        }
        
        /// <summary>
        /// Update environmental factors based on time of day
        /// </summary>
        private void UpdateForTimeOfDay()
        {
            // Light quality changes with time of day
            LightQuality = CurrentTimeOfDay switch
            {
                TimeOfDay.Morning => 80,
                TimeOfDay.Day => 100,
                TimeOfDay.Evening => 70,
                TimeOfDay.Night => 10,
                _ => 70
            };
            
            // Temperature varies throughout the day
            double tempAdjustment = CurrentTimeOfDay switch
            {
                TimeOfDay.Morning => -5,
                TimeOfDay.Day => +5,
                TimeOfDay.Evening => 0,
                TimeOfDay.Night => -10,
                _ => 0
            };
            
            Temperature += tempAdjustment;
        }
        
        /// <summary>
        /// Update humidity based on current weather
        /// </summary>
        private void UpdateHumidityForWeather()
        {
            // Base humidity by weather type
            Humidity = CurrentWeather switch
            {
                Weather.Rain => 90,
                Weather.Humid => 85,
                Weather.Storm => 95,
                Weather.Snow => 70,
                Weather.Cloudy => 65,
                Weather.Sunny => 40,
                Weather.Wind => 30,
                _ => 50
            };
            
            // Climate factor
            Humidity += CurrentClimate switch
            {
                ClimateZone.Tropical => 20,
                ClimateZone.Desert => -20,
                _ => 0
            };
            
            // Ensure valid range
            Humidity = Math.Max(0, Math.Min(100, Humidity));
        }
        
        /// <summary>
        /// Update environmental conditions based on all factors
        /// </summary>
        private void UpdateEnvironmentalConditions()
        {
            // Update soil quality based on current conditions
            UpdateSoilQuality();
            
            // Update air quality based on current conditions
            UpdateAirQuality();
            
            // Apply any active environmental events
            ApplyActiveEvents();
        }
        
        /// <summary>
        /// Update soil quality based on current conditions
        /// </summary>
        private void UpdateSoilQuality()
        {
            // Soil quality is affected by weather and events
            double soilChange = 0;
            
            // Weather effects on soil
            soilChange += CurrentWeather switch
            {
                Weather.Rain => +0.1, // Rain improves soil slightly
                Weather.Storm => -0.1, // Storms can cause erosion
                Weather.Sunny when Humidity < 30 => -0.2, // Hot, dry weather degrades soil
                _ => -0.05 // Slight general degradation
            };
            
            // Apply change
            SoilQuality = Math.Max(0, Math.Min(100, SoilQuality + soilChange));
        }
        
        /// <summary>
        /// Update air quality based on current conditions
        /// </summary>
        private void UpdateAirQuality()
        {
            // Air quality is affected by weather and events
            double airChange = 0;
            
            // Weather effects on air
            airChange += CurrentWeather switch
            {
                Weather.Rain => +0.2, // Rain cleans the air
                Weather.Wind => +0.3, // Wind refreshes air
                Weather.Storm => +0.4, // Storms clean air but can be stressful
                _ => -0.05 // Slight general degradation
            };
            
            // Apply change
            AirQuality = Math.Max(0, Math.Min(100, AirQuality + airChange));
        }
        
        #endregion
        
        #region Event Management
        
        /// <summary>
        /// Generate seasonal events for the current season
        /// </summary>
        private void GenerateSeasonalEvents()
        {
            // Clear upcoming events
            UpcomingEvents.Clear();
            
            // Generate 1-3 random events for this season
            int eventCount = random.Next(1, 4);
            
            for (int i = 0; i < eventCount; i++)
            {
                // Delay between 5-25 days
                int delayDays = random.Next(5, 26);
                
                // Duration between 1-5 days
                int durationDays = random.Next(1, 6);
                
                // Create event start time
                DateTime eventStart = DateTime.Now.AddDays(delayDays);
                
                // Select appropriate event for the season
                EventType eventType = GetRandomEventForSeason(CurrentSeason);
                
                // Create the event
                var environmentalEvent = new EnvironmentalEvent
                {
                    Type = eventType,
                    StartTime = eventStart,
                    Duration = TimeSpan.FromDays(durationDays),
                    Intensity = random.Next(30, 91) // 30-90 intensity
                };
                
                // Add to upcoming events
                UpcomingEvents.Add(environmentalEvent);
            }
        }
        
        /// <summary>
        /// Get a random event type appropriate for the current season
        /// </summary>
        private EventType GetRandomEventForSeason(Season season)
        {
            double roll = random.NextDouble();
            
            return season switch
            {
                Season.Spring => roll switch
                {
                    < 0.3 => EventType.HeavyRain,
                    < 0.5 => EventType.Pollen,
                    < 0.7 => EventType.MildTemperatures,
                    < 0.9 => EventType.Insects,
                    _ => EventType.FreakWeather
                },
                
                Season.Summer => roll switch
                {
                    < 0.3 => EventType.Heatwave,
                    < 0.5 => EventType.Drought,
                    < 0.7 => EventType.Insects,
                    < 0.9 => EventType.SunnyPeriod,
                    _ => EventType.FreakWeather
                },
                
                Season.Autumn => roll switch
                {
                    < 0.3 => EventType.LeafFall,
                    < 0.5 => EventType.WindyPeriod,
                    < 0.7 => EventType.MildTemperatures,
                    < 0.9 => EventType.EarlyFrost,
                    _ => EventType.FreakWeather
                },
                
                Season.Winter => roll switch
                {
                    < 0.3 => EventType.Frost,
                    < 0.5 => EventType.Snowfall,
                    < 0.7 => EventType.ColdSnap,
                    < 0.9 => EventType.LowLight,
                    _ => EventType.FreakWeather
                },
                
                _ => EventType.MildTemperatures
            };
        }
        
        /// <summary>
        /// Check for environmental events that should start or end
        /// </summary>
        private void CheckForEnvironmentalEvents()
        {
            DateTime now = DateTime.Now;
            
            // Check for events that should start
            foreach (var upcomingEvent in UpcomingEvents.ToArray())
            {
                if (now >= upcomingEvent.StartTime)
                {
                    // Event starts
                    ActiveEvents.Add(upcomingEvent);
                    UpcomingEvents.Remove(upcomingEvent);
                    
                    // Trigger event started
                    EnvironmentalEventStarted?.Invoke(this, new EnvironmentalEventArgs(upcomingEvent));
                }
            }
            
            // Check for events that should end
            foreach (var activeEvent in ActiveEvents.ToArray())
            {
                if (now >= activeEvent.StartTime + activeEvent.Duration)
                {
                    // Event ends
                    ActiveEvents.Remove(activeEvent);
                    
                    // Trigger event ended
                    EnvironmentalEventEnded?.Invoke(this, new EnvironmentalEventArgs(activeEvent));
                }
            }
        }
        
        /// <summary>
        /// Apply effects of all active environmental events
        /// </summary>
        private void ApplyActiveEvents()
        {
            foreach (var activeEvent in ActiveEvents)
            {
                // Apply event effects based on type and intensity
                ApplyEventEffects(activeEvent);
            }
        }
        
        /// <summary>
        /// Apply the effects of an environmental event
        /// </summary>
        private void ApplyEventEffects(EnvironmentalEvent environmentalEvent)
        {
            // Scale effects based on intensity (0-100)
            double intensityFactor = environmentalEvent.Intensity / 100.0;
            
            switch (environmentalEvent.Type)
            {
                case EventType.Heatwave:
                    Temperature += 15 * intensityFactor;
                    Humidity -= 20 * intensityFactor;
                    break;
                    
                case EventType.Drought:
                    Humidity -= 30 * intensityFactor;
                    SoilQuality -= 1.0 * intensityFactor;
                    break;
                    
                case EventType.HeavyRain:
                    Humidity += 30 * intensityFactor;
                    SoilQuality += 0.5 * intensityFactor;
                    break;
                    
                case EventType.ColdSnap:
                    Temperature -= 20 * intensityFactor;
                    break;
                    
                case EventType.Frost:
                    Temperature -= 10 * intensityFactor;
                    break;
                    
                case EventType.Snowfall:
                    Humidity += 10 * intensityFactor;
                    Temperature -= 5 * intensityFactor;
                    LightQuality -= 20 * intensityFactor;
                    break;
                    
                case EventType.WindyPeriod:
                    Humidity -= 10 * intensityFactor;
                    AirQuality += 1.0 * intensityFactor;
                    break;
                    
                case EventType.SunnyPeriod:
                    LightQuality += 10 * intensityFactor;
                    Temperature += 5 * intensityFactor;
                    break;
                    
                case EventType.MildTemperatures:
                    // Mild temperatures are neutral
                    break;
                    
                case EventType.LowLight:
                    LightQuality -= 30 * intensityFactor;
                    break;
                    
                case EventType.Pollen:
                    AirQuality -= 10 * intensityFactor;
                    break;
                    
                case EventType.Insects:
                    // Insects are handled by the bonsai pet system
                    break;
                    
                case EventType.LeafFall:
                    // Leaf fall is visual only
                    break;
                    
                case EventType.EarlyFrost:
                    Temperature -= 8 * intensityFactor;
                    break;
                    
                case EventType.FreakWeather:
                    // Random extreme event
                    if (random.NextDouble() < 0.5)
                    {
                        Temperature += 25 * intensityFactor;
                        Humidity -= 30 * intensityFactor;
                    }
                    else
                    {
                        Temperature -= 25 * intensityFactor;
                        Humidity += 30 * intensityFactor;
                    }
                    break;
            }
            
            // Clamp values to valid ranges
            Temperature = Math.Max(-20, Math.Min(120, Temperature));
            Humidity = Math.Max(0, Math.Min(100, Humidity));
            SoilQuality = Math.Max(0, Math.Min(100, SoilQuality));
            AirQuality = Math.Max(0, Math.Min(100, AirQuality));
            LightQuality = Math.Max(0, Math.Min(100, LightQuality));
        }
        
        #endregion
        
        #region Climate Management
        
        /// <summary>
        /// Set the climate zone
        /// </summary>
        public void SetClimate(ClimateZone climate)
        {
            if (CurrentClimate == climate)
                return;
            
            CurrentClimate = climate;
            
            // Update temperature ranges and base values for the new climate
            UpdateClimateSettings();
            
            // Update weather to be appropriate for the climate
            CurrentWeather = GetRandomWeatherForSeason(CurrentSeason, CurrentClimate);
            
            // Update weather forecast
            GenerateWeatherForecast();
            
            // Trigger weather changed event
            WeatherChanged?.Invoke(this, new WeatherChangedEventArgs(CurrentWeather, Temperature));
        }
        
        /// <summary>
        /// Update settings for the current climate
        /// </summary>
        private void UpdateClimateSettings()
        {
            // Update temperature based on climate
            Temperature = CurrentClimate switch
            {
                ClimateZone.Desert => 85 + random.Next(-5, 6),
                ClimateZone.Tropical => 82 + random.Next(-3, 4),
                ClimateZone.Temperate => 72 + random.Next(-7, 8),
                ClimateZone.Alpine => 55 + random.Next(-10, 11),
                _ => 72
            };
            
            // Apply seasonal adjustment
            Temperature += CurrentSeason switch
            {
                Season.Winter => -20,
                Season.Spring => -5,
                Season.Summer => +10,
                Season.Autumn => -10,
                _ => 0
            };
            
            // Update humidity based on climate
            Humidity = CurrentClimate switch
            {
                ClimateZone.Desert => 20 + random.Next(0, 11),
                ClimateZone.Tropical => 80 + random.Next(0, 11),
                ClimateZone.Temperate => 50 + random.Next(-10, 11),
                ClimateZone.Alpine => 40 + random.Next(-5, 6),
                _ => 50
            };
            
            // Weather adjustment to humidity
            UpdateHumidityForWeather();
        }
        
        /// <summary>
        /// Set the time multiplier for game time progression
        /// </summary>
        public void SetTimeMultiplier(double multiplier)
        {
            // Limit multiplier to reasonable range
            TimeMultiplier = Math.Max(0.1, Math.Min(100, multiplier));
        }
        
        #endregion
    }
    
    #region Support Classes and Enums
    
    /// <summary>
    /// Weather forecast data
    /// </summary>
    public class WeatherForecast
    {
        public Weather Weather { get; set; }
        public int Day { get; set; }  // 0 = today, 1 = tomorrow, etc.
        public double Temperature { get; set; }
        public int Probability { get; set; } // 0-100%
    }
    
    /// <summary>
    /// Environmental event data
    /// </summary>
    public class EnvironmentalEvent
    {
        public EventType Type { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public double Intensity { get; set; }  // 0-100
        
        public string GetDescription()
        {
            string intensityText = Intensity switch
            {
                >= 80 => "Extreme",
                >= 60 => "Severe",
                >= 40 => "Moderate",
                _ => "Mild"
            };
            
            return $"{intensityText} {Type}";
        }
    }
    
    /// <summary>
    /// Event args for season changes
    /// </summary>
    public class SeasonChangedEventArgs : EventArgs
    {
        public Season Season { get; }
        
        public SeasonChangedEventArgs(Season season)
        {
            Season = season;
        }
    }
    
    /// <summary>
    /// Event args for weather changes
    /// </summary>
    public class WeatherChangedEventArgs : EventArgs
    {
        public Weather Weather { get; }
        public double Temperature { get; }
        
        public WeatherChangedEventArgs(Weather weather, double temperature)
        {
            Weather = weather;
            Temperature = temperature;
        }
    }
    
    /// <summary>
    /// Event args for time of day changes
    /// </summary>
    public class TimeOfDayChangedEventArgs : EventArgs
    {
        public TimeOfDay CurrentTimeOfDay { get; }
        public TimeOfDay PreviousTimeOfDay { get; }
        public int CurrentHour { get; }
        
        public TimeOfDayChangedEventArgs(TimeOfDay current, TimeOfDay previous, int hour)
        {
            CurrentTimeOfDay = current;
            PreviousTimeOfDay = previous;
            CurrentHour = hour;
        }
    }
    
    /// <summary>
    /// Event args for environmental events
    /// </summary>
    public class EnvironmentalEventArgs : EventArgs
    {
        public EnvironmentalEvent Event { get; }
        
        public EnvironmentalEventArgs(EnvironmentalEvent environmentalEvent)
        {
            Event = environmentalEvent;
        }
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
    /// Seasons
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
    
    /// <summary>
    /// Weather conditions
    /// </summary>
    public enum Weather
    {
        Sunny,
        Cloudy,
        Rain,
        Humid,
        Wind,
        Storm,
        Snow
    }
    
    /// <summary>
    /// Times of day
    /// </summary>
    public enum TimeOfDay
    {
        Morning,
        Day,
        Evening,
        Night
    }
    
    /// <summary>
    /// Environmental event types
    /// </summary>
    public enum EventType
    {
        Heatwave,
        Drought,
        HeavyRain,
        ColdSnap,
        Frost,
        Snowfall,
        WindyPeriod,
        SunnyPeriod,
        MildTemperatures,
        LowLight,
        Pollen,
        Insects,
        LeafFall,
        EarlyFrost,
        FreakWeather
    }
    
    #endregion
}