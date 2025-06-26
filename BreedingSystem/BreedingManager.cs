using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BonsaiGotchi.BreedingSystem
{
    /// <summary>
    /// Manages the breeding and collection of bonsai trees
    /// </summary>
    public class BreedingManager
    {
        // Collection of owned bonsai trees
        public List<BonsaiSpecimen> Collection { get; private set; } = new List<BonsaiSpecimen>();
        
        // Seeds from breeding
        public List<BonsaiSeed> Seeds { get; private set; } = new List<BonsaiSeed>();
        
        // Statistics
        public int TotalBreedingAttempts { get; private set; } = 0;
        public int SuccessfulBreedings { get; private set; } = 0;
        
        // Current active bonsai (the one being displayed)
        public BonsaiPet ActiveBonsai { get; private set; }
        
        private readonly Random random;
        
        /// <summary>
        /// Initialize the breeding manager
        /// </summary>
        public BreedingManager(Random random, BonsaiPet initialBonsai = null)
        {
            this.random = random;
            
            // Add the initial bonsai to the collection if provided
            if (initialBonsai != null)
            {
                ActiveBonsai = initialBonsai;
                AddToCollection(initialBonsai);
            }
        }
        
        #region Collection Management
        
        /// <summary>
        /// Add a bonsai to the collection
        /// </summary>
        public void AddToCollection(BonsaiPet bonsai)
        {
            if (bonsai == null) return;
            
            // Check if already in collection by ID
            if (Collection.Any(b => b.Id == bonsai.Id))
            {
                return; // Already in collection
            }
            
            // Create specimen record
            var specimen = new BonsaiSpecimen
            {
                Id = bonsai.Id,
                Name = bonsai.Name,
                AcquisitionDate = DateTime.Now,
                Age = bonsai.Age,
                Stage = bonsai.CurrentStage,
                Genetics = new BonsaiGenetics(random), // Create new genetics for existing bonsai
                BonsaiInstance = bonsai
            };
            
            // Add to collection
            Collection.Add(specimen);
        }
        
        /// <summary>
        /// Remove a bonsai from the collection
        /// </summary>
        public bool RemoveFromCollection(Guid bonsaiId)
        {
            int initialCount = Collection.Count;
            Collection.RemoveAll(b => b.Id == bonsaiId);
            
            return Collection.Count < initialCount;
        }
        
        /// <summary>
        /// Get a specimen from the collection
        /// </summary>
        public BonsaiSpecimen GetSpecimen(Guid bonsaiId)
        {
            return Collection.FirstOrDefault(b => b.Id == bonsaiId);
        }
        
        /// <summary>
        /// Set the active bonsai
        /// </summary>
        public void SetActiveBonsai(BonsaiPet bonsai)
        {
            if (bonsai == null) return;
            
            // Add to collection if not already there
            if (!Collection.Any(b => b.Id == bonsai.Id))
            {
                AddToCollection(bonsai);
            }
            
            ActiveBonsai = bonsai;
        }
        
        #endregion
        
        #region Breeding Methods
        
        /// <summary>
        /// Attempt to breed two bonsai trees
        /// </summary>
        /// <returns>The resulting seed if successful, null otherwise</returns>
        public BonsaiSeed TryBreed(Guid parent1Id, Guid parent2Id)
        {
            TotalBreedingAttempts++;
            
            // Find parents in collection
            var parent1 = Collection.FirstOrDefault(b => b.Id == parent1Id);
            var parent2 = Collection.FirstOrDefault(b => b.Id == parent2Id);
            
            if (parent1 == null || parent2 == null)
            {
                return null; // Parents not found
            }
            
            // Check if both parents are mature enough
            if (parent1.Stage < GrowthStage.MatureTree || parent2.Stage < GrowthStage.MatureTree)
            {
                return null; // Parents not mature enough
            }
            
            // Calculate breeding success chance
            double compatibility = parent1.Genetics.CalculateBreedingCompatibility(parent2.Genetics);
            double successChance = CalculateBreedingSuccessChance(parent1, parent2, compatibility);
            
            // Roll for success
            if (random.NextDouble() > successChance)
            {
                return null; // Breeding failed
            }
            
            // Breeding successful!
            SuccessfulBreedings++;
            
            // Create seed with combined genetics
            var seed = new BonsaiSeed
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                Genetics = new BonsaiGenetics(parent1.Genetics, parent2.Genetics, random),
                Parent1Id = parent1Id,
                Parent2Id = parent2Id,
                Parent1Name = parent1.Name,
                Parent2Name = parent2.Name,
                Rarity = DetermineSeedRarity(parent1, parent2)
            };
            
            // Add to seed collection
            Seeds.Add(seed);
            
            return seed;
        }
        
        /// <summary>
        /// Calculate the chance of successful breeding
        /// </summary>
        private double CalculateBreedingSuccessChance(
            BonsaiSpecimen parent1, 
            BonsaiSpecimen parent2,
            double compatibility)
        {
            // Base success chance based on compatibility (0.1 to 0.6)
            double baseChance = 0.1 + (compatibility * 0.5);
            
            // Adjust for parent health
            double health1 = parent1.BonsaiInstance?.Health ?? 50;
            double health2 = parent2.BonsaiInstance?.Health ?? 50;
            double healthModifier = ((health1 + health2) / 200) * 0.2; // Up to 0.2 bonus
            
            // Adjust for parent age
            double ageModifier = 0;
            if (parent1.Stage == GrowthStage.ElderTree && parent2.Stage == GrowthStage.ElderTree)
            {
                ageModifier = 0.1; // Elder trees have better chance
            }
            
            // Experience bonus from previous successful breedings
            double experienceBonus = Math.Min(0.1, SuccessfulBreedings * 0.01); // Up to 0.1
            
            double finalChance = baseChance + healthModifier + ageModifier + experienceBonus;
            
            // Ensure chance is between 0.05 and 0.9
            return Math.Max(0.05, Math.Min(0.9, finalChance));
        }
        
        /// <summary>
        /// Determine the rarity of the resulting seed
        /// </summary>
        private SeedRarity DetermineSeedRarity(BonsaiSpecimen parent1, BonsaiSpecimen parent2)
        {
            // Base rarity on parent genetics and a random factor
            int rarityScore = 0;
            
            // Parent rarity contribution
            rarityScore += (int)parent1.Genetics.Rarity * 2;
            rarityScore += (int)parent2.Genetics.Rarity * 2;
            
            // Special trait contribution
            rarityScore += parent1.Genetics.SpecialTraits.Count;
            rarityScore += parent2.Genetics.SpecialTraits.Count;
            
            // Random factor
            rarityScore += random.Next(5);
            
            // Determine rarity based on score
            if (rarityScore >= 20)
            {
                return SeedRarity.Mythic;
            }
            else if (rarityScore >= 15)
            {
                return SeedRarity.Legendary;
            }
            else if (rarityScore >= 10)
            {
                return SeedRarity.Rare;
            }
            else if (rarityScore >= 5)
            {
                return SeedRarity.Uncommon;
            }
            else
            {
                return SeedRarity.Common;
            }
        }
        
        /// <summary>
        /// Plant a seed to create a new bonsai
        /// </summary>
        public BonsaiPet PlantSeed(Guid seedId, string name)
        {
            // Find the seed in collection
            var seed = Seeds.FirstOrDefault(s => s.Id == seedId);
            if (seed == null) return null;
            
            // Remove the seed from collection
            Seeds.Remove(seed);
            
            // Create a new bonsai with the seed's genetics
            var bonsai = new BonsaiPet(name, random);
            
            // Apply the seed's genetics to the bonsai
            ApplySeedGenetics(bonsai, seed);
            
            // Add the new bonsai to the collection
            AddToCollection(bonsai);
            
            return bonsai;
        }
        
        /// <summary>
        /// Apply seed genetics to a bonsai
        /// </summary>
        private void ApplySeedGenetics(BonsaiPet bonsai, BonsaiSeed seed)
        {
            // Apply style based on genetics
            bonsai.Style = seed.Genetics.DominantStyle;
            
            // Apply species to traits
            string[] traits = seed.Genetics.Species.Split(' ');
            bonsai.Traits.AddRange(traits);
            
            // Apply special traits to likes/dislikes
            foreach (var specialTrait in seed.Genetics.SpecialTraits)
            {
                switch (specialTrait)
                {
                    case SpecialTrait.RedLeaves:
                        bonsai.Traits.Add("Red-Leafed");
                        break;
                    case SpecialTrait.VarlegetedLeaves:
                        bonsai.Traits.Add("Variegated");
                        break;
                    case SpecialTrait.DwarfGrowth:
                        bonsai.Traits.Add("Dwarf");
                        break;
                    case SpecialTrait.GiantGrowth:
                        bonsai.Traits.Add("Giant");
                        break;
                    case SpecialTrait.EarlyFlowering:
                        bonsai.Traits.Add("Early Bloomer");
                        break;
                    case SpecialTrait.WeepingForm:
                        bonsai.Traits.Add("Weeping");
                        break;
                }
            }
            
            // Apply genetic traits to stats
            if (seed.Genetics.Genes.TryGetValue(GeneticTrait.DroughtTolerance, out int droughtTolerance))
            {
                // High drought tolerance means slower water loss
                bonsai.WaterNeeds = droughtTolerance > 70 ? 
                    WaterPreference.Low :
                    droughtTolerance < 30 ? 
                        WaterPreference.High : 
                        WaterPreference.Moderate;
            }
            
            if (seed.Genetics.Genes.TryGetValue(GeneticTrait.PestResistance, out int pestResistance))
            {
                // Modify starting pest infestation based on resistance
                bonsai.PestInfestation = Math.Max(0, 10 - (pestResistance / 10));
            }
            
            if (seed.Genetics.Genes.TryGetValue(GeneticTrait.DiseaseResistance, out int diseaseResistance))
            {
                // Modify starting disease level based on resistance
                bonsai.DiseaseLevel = Math.Max(0, 10 - (diseaseResistance / 10));
            }
        }
        
        /// <summary>
        /// Collect seeds from a mature bonsai
        /// </summary>
        public BonsaiSeed CollectSeed(Guid bonsaiId)
        {
            // Find the bonsai in collection
            var specimen = Collection.FirstOrDefault(b => b.Id == bonsaiId);
            if (specimen == null) return null;
            
            // Check if mature enough
            if (specimen.Stage < GrowthStage.MatureTree)
            {
                return null; // Not mature enough
            }
            
            // Check if healthy enough
            if (specimen.BonsaiInstance?.Health < 60)
            {
                return null; // Not healthy enough
            }
            
            // Create a seed with the bonsai's genetics (self-pollination)
            var seed = new BonsaiSeed
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                Genetics = new BonsaiGenetics(specimen.Genetics, specimen.Genetics, random), // Self-pollination
                Parent1Id = bonsaiId,
                Parent2Id = bonsaiId, // Same parent
                Parent1Name = specimen.Name,
                Parent2Name = specimen.Name,
                Rarity = SeedRarity.Common // Self-pollinated seeds are always common
            };
            
            // Add to seed collection
            Seeds.Add(seed);
            
            return seed;
        }
        
        #endregion
        
        #region Save/Load Methods
        
        /// <summary>
        /// Save the breeding system data
        /// </summary>
        public async Task SaveDataAsync(string filePath)
        {
            // Create save data object
            var saveData = new BreedingSystemSaveData
            {
                Seeds = Seeds,
                TotalBreedingAttempts = TotalBreedingAttempts,
                SuccessfulBreedings = SuccessfulBreedings,
                
                // Only save specimen data, not bonsai instances
                CollectionData = Collection.Select(s => new BonsaiSpecimenData
                {
                    Id = s.Id,
                    Name = s.Name,
                    AcquisitionDate = s.AcquisitionDate,
                    Age = s.Age,
                    Stage = s.Stage,
                    Genetics = s.Genetics
                }).ToList()
            };
            
            // Serialize and save
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(saveData, options);
            
            await File.WriteAllTextAsync(filePath, json);
        }
        
        /// <summary>
        /// Load the breeding system data
        /// </summary>
        public async Task<bool> LoadDataAsync(string filePath)
        {
            try
            {
                // Load and deserialize
                string json = await File.ReadAllTextAsync(filePath);
                var saveData = JsonSerializer.Deserialize<BreedingSystemSaveData>(json);
                
                if (saveData == null)
                {
                    return false;
                }
                
                // Restore data
                Seeds = saveData.Seeds;
                TotalBreedingAttempts = saveData.TotalBreedingAttempts;
                SuccessfulBreedings = saveData.SuccessfulBreedings;
                
                // Convert specimen data back to specimens
                Collection.Clear();
                foreach (var data in saveData.CollectionData)
                {
                    var specimen = new BonsaiSpecimen
                    {
                        Id = data.Id,
                        Name = data.Name,
                        AcquisitionDate = data.AcquisitionDate,
                        Age = data.Age,
                        Stage = data.Stage,
                        Genetics = data.Genetics
                    };
                    
                    Collection.Add(specimen);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load breeding data: {ex.Message}");
                return false;
            }
        }
        
        #endregion
    }
    
    #region Support Classes
    
    /// <summary>
    /// Represents a bonsai tree specimen in the collection
    /// </summary>
    public class BonsaiSpecimen
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime AcquisitionDate { get; set; }
        public int Age { get; set; }
        public GrowthStage Stage { get; set; }
        public BonsaiGenetics Genetics { get; set; }
        
        [System.Text.Json.Serialization.JsonIgnore]
        public BonsaiPet BonsaiInstance { get; set; }
    }
    
    /// <summary>
    /// Save data for a bonsai specimen (without the pet instance)
    /// </summary>
    public class BonsaiSpecimenData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime AcquisitionDate { get; set; }
        public int Age { get; set; }
        public GrowthStage Stage { get; set; }
        public BonsaiGenetics Genetics { get; set; }
    }
    
    /// <summary>
    /// Represents a seed from breeding
    /// </summary>
    public class BonsaiSeed
    {
        public Guid Id { get; set; }
        public DateTime CreationDate { get; set; }
        public BonsaiGenetics Genetics { get; set; }
        public Guid Parent1Id { get; set; }
        public Guid Parent2Id { get; set; }
        public string Parent1Name { get; set; }
        public string Parent2Name { get; set; }
        public SeedRarity Rarity { get; set; }
        public bool IsGerminating { get; set; }
        public int GerminationProgress { get; set; }
    }
    
    /// <summary>
    /// Save data for the breeding system
    /// </summary>
    public class BreedingSystemSaveData
    {
        public List<BonsaiSeed> Seeds { get; set; }
        public List<BonsaiSpecimenData> CollectionData { get; set; }
        public int TotalBreedingAttempts { get; set; }
        public int SuccessfulBreedings { get; set; }
    }
    
    /// <summary>
    /// Rarity levels for seeds
    /// </summary>
    public enum SeedRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
        Mythic
    }
    
    #endregion
}