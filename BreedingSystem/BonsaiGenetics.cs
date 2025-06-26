using System;
using System.Collections.Generic;

namespace BonsaiGotchi.BreedingSystem
{
    /// <summary>
    /// Manages the genetics and traits for bonsai breeding
    /// </summary>
    public class BonsaiGenetics
    {
        // Core genetic traits
        public Dictionary<GeneticTrait, int> Genes { get; set; } = new Dictionary<GeneticTrait, int>();
        
        // Rarity and special traits
        public List<SpecialTrait> SpecialTraits { get; set; } = new List<SpecialTrait>();
        public BonsaiRarity Rarity { get; set; } = BonsaiRarity.Common;
        
        // Dominant style based on genetics
        public BonsaiStyle DominantStyle { get; private set; } = BonsaiStyle.FormalUpright;
        
        // Species identification
        public string Species { get; set; } = "Juniper";
        
        private readonly Random random;
        
        /// <summary>
        /// Create a new bonsai genetic profile
        /// </summary>
        public BonsaiGenetics(Random random)
        {
            this.random = random;
            InitializeRandomGenetics();
        }
        
        /// <summary>
        /// Create a bonsai genetic profile by breeding two parents
        /// </summary>
        public BonsaiGenetics(BonsaiGenetics parent1, BonsaiGenetics parent2, Random random)
        {
            this.random = random;
            
            // Inherit genes from both parents with some mutation
            InheritGenes(parent1, parent2);
            
            // Determine special traits (chance to inherit or develop new ones)
            InheritSpecialTraits(parent1, parent2);
            
            // Calculate rarity based on genes and special traits
            CalculateRarity();
            
            // Determine species based on parents
            DetermineSpecies(parent1, parent2);
            
            // Calculate dominant style based on genes
            CalculateDominantStyle();
        }
        
        #region Genetics Initialization
        
        /// <summary>
        /// Initialize random genetics for a new bonsai
        /// </summary>
        private void InitializeRandomGenetics()
        {
            // Initialize all genetic traits with random values (0-100)
            foreach (GeneticTrait trait in Enum.GetValues(typeof(GeneticTrait)))
            {
                Genes[trait] = random.Next(101);
            }
            
            // Small chance for special traits
            foreach (SpecialTrait trait in Enum.GetValues(typeof(SpecialTrait)))
            {
                if (random.NextDouble() < 0.05) // 5% chance for each special trait
                {
                    SpecialTraits.Add(trait);
                }
            }
            
            // Set species randomly
            Species = GetRandomSpecies();
            
            // Calculate rarity
            CalculateRarity();
            
            // Calculate dominant style based on genes
            CalculateDominantStyle();
        }
        
        /// <summary>
        /// Get a random bonsai species
        /// </summary>
        private string GetRandomSpecies()
        {
            string[] commonSpecies = {
                "Juniper", "Chinese Elm", "Ficus", "Japanese Maple",
                "Pine", "Jade", "Boxwood"
            };
            
            string[] uncommonSpecies = {
                "Azalea", "Bougainvillea", "Cotoneaster", "Wisteria", 
                "Pomegranate", "Olive"
            };
            
            string[] rareSpecies = {
                "Black Pine", "Cedar", "Ginkgo", "Japanese White Pine",
                "Satsuki Azalea"
            };
            
            double roll = random.NextDouble();
            
            if (roll < 0.7) // 70% common
            {
                return commonSpecies[random.Next(commonSpecies.Length)];
            }
            else if (roll < 0.95) // 25% uncommon
            {
                return uncommonSpecies[random.Next(uncommonSpecies.Length)];
            }
            else // 5% rare
            {
                return rareSpecies[random.Next(rareSpecies.Length)];
            }
        }
        
        #endregion
        
        #region Breeding Methods
        
        /// <summary>
        /// Inherit genes from both parents with mutation chance
        /// </summary>
        private void InheritGenes(BonsaiGenetics parent1, BonsaiGenetics parent2)
        {
            foreach (GeneticTrait trait in Enum.GetValues(typeof(GeneticTrait)))
            {
                // Get parent values
                int value1 = parent1.Genes.TryGetValue(trait, out int v1) ? v1 : 50;
                int value2 = parent2.Genes.TryGetValue(trait, out int v2) ? v2 : 50;
                
                // Inherit with some randomness
                int inheritedValue;
                double roll = random.NextDouble();
                
                if (roll < 0.45) // 45% chance to inherit from parent 1
                {
                    inheritedValue = value1;
                }
                else if (roll < 0.9) // 45% chance to inherit from parent 2
                {
                    inheritedValue = value2;
                }
                else // 10% chance to blend
                {
                    inheritedValue = (value1 + value2) / 2;
                }
                
                // Apply mutation
                if (random.NextDouble() < 0.15) // 15% mutation chance
                {
                    // Mutation can be +/- up to 15 points
                    int mutation = random.Next(-15, 16);
                    inheritedValue += mutation;
                    
                    // Ensure value is in valid range
                    inheritedValue = Math.Max(0, Math.Min(100, inheritedValue));
                }
                
                // Store the gene
                Genes[trait] = inheritedValue;
            }
        }
        
        /// <summary>
        /// Inherit or develop special traits
        /// </summary>
        private void InheritSpecialTraits(BonsaiGenetics parent1, BonsaiGenetics parent2)
        {
            // Check each special trait
            foreach (SpecialTrait trait in Enum.GetValues(typeof(SpecialTrait)))
            {
                bool parent1HasTrait = parent1.SpecialTraits.Contains(trait);
                bool parent2HasTrait = parent2.SpecialTraits.Contains(trait);
                
                if (parent1HasTrait && parent2HasTrait)
                {
                    // Both parents have the trait - high chance to inherit
                    if (random.NextDouble() < 0.8) // 80% chance
                    {
                        SpecialTraits.Add(trait);
                    }
                }
                else if (parent1HasTrait || parent2HasTrait)
                {
                    // One parent has the trait - medium chance to inherit
                    if (random.NextDouble() < 0.4) // 40% chance
                    {
                        SpecialTraits.Add(trait);
                    }
                }
                else
                {
                    // Neither parent has the trait - small chance to develop
                    if (random.NextDouble() < 0.02) // 2% chance
                    {
                        SpecialTraits.Add(trait);
                    }
                }
            }
        }
        
        /// <summary>
        /// Determine species based on parents
        /// </summary>
        private void DetermineSpecies(BonsaiGenetics parent1, BonsaiGenetics parent2)
        {
            if (parent1.Species == parent2.Species)
            {
                // Same species - child inherits the species
                Species = parent1.Species;
            }
            else
            {
                // Different species - random inheritance with hybrid chance
                if (random.NextDouble() < 0.1) // 10% chance of hybrid
                {
                    Species = $"Hybrid {parent1.Species}-{parent2.Species}";
                }
                else
                {
                    // Inherit from one parent
                    Species = random.NextDouble() < 0.5 ? parent1.Species : parent2.Species;
                }
            }
        }
        
        /// <summary>
        /// Calculate the rarity based on genes and special traits
        /// </summary>
        private void CalculateRarity()
        {
            // Base rarity on number of special traits and extreme gene values
            int rarityScore = 0;
            
            // Add points for each special trait
            rarityScore += SpecialTraits.Count * 2;
            
            // Check for extreme gene values (very high or very low)
            foreach (var gene in Genes)
            {
                if (gene.Value >= 90 || gene.Value <= 10)
                {
                    rarityScore++;
                }
            }
            
            // Determine rarity based on score
            if (rarityScore >= 8)
            {
                Rarity = BonsaiRarity.Legendary;
            }
            else if (rarityScore >= 5)
            {
                Rarity = BonsaiRarity.Rare;
            }
            else if (rarityScore >= 3)
            {
                Rarity = BonsaiRarity.Uncommon;
            }
            else
            {
                Rarity = BonsaiRarity.Common;
            }
        }
        
        /// <summary>
        /// Calculate the dominant style based on genetic traits
        /// </summary>
        private void CalculateDominantStyle()
        {
            // Each style is associated with certain genetic traits
            Dictionary<BonsaiStyle, int> styleScores = new();
            
            // FormalUpright - associated with height and straightness
            styleScores[BonsaiStyle.FormalUpright] = 
                (Genes.TryGetValue(GeneticTrait.Height, out int height) ? height : 50) +
                (Genes.TryGetValue(GeneticTrait.Straightness, out int straight) ? straight : 50);
                
            // InformalUpright - associated with flexibility and asymmetry
            styleScores[BonsaiStyle.InformalUpright] = 
                (Genes.TryGetValue(GeneticTrait.Flexibility, out int flex) ? flex : 50) +
                (100 - (Genes.TryGetValue(GeneticTrait.Symmetry, out int sym) ? sym : 50));
                
            // Windswept - associated with flexibility and resilience
            styleScores[BonsaiStyle.Windswept] = 
                (Genes.TryGetValue(GeneticTrait.Flexibility, out int wflex) ? wflex : 50) +
                (Genes.TryGetValue(GeneticTrait.Resilience, out int res) ? res : 50);
                
            // Cascade - associated with drooping and negative height
            styleScores[BonsaiStyle.Cascade] = 
                (Genes.TryGetValue(GeneticTrait.Drooping, out int droop) ? droop : 50) +
                (100 - (Genes.TryGetValue(GeneticTrait.Height, out int cheight) ? cheight : 50));
                
            // Slanting - associated with lean and asymmetry
            styleScores[BonsaiStyle.Slanting] = 
                (Genes.TryGetValue(GeneticTrait.Lean, out int lean) ? lean : 50) +
                (100 - (Genes.TryGetValue(GeneticTrait.Symmetry, out int ssym) ? ssym : 50));
                
            // Find the style with the highest score
            int maxScore = 0;
            BonsaiStyle maxStyle = BonsaiStyle.FormalUpright; // Default
            
            foreach (var style in styleScores)
            {
                if (style.Value > maxScore)
                {
                    maxScore = style.Value;
                    maxStyle = style.Key;
                }
            }
            
            DominantStyle = maxStyle;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Calculate compatibility for breeding with another bonsai
        /// </summary>
        public double CalculateBreedingCompatibility(BonsaiGenetics other)
        {
            if (other == null) return 0;
            
            double compatibility = 0;
            
            // Species compatibility
            if (Species == other.Species)
            {
                compatibility += 0.5; // Same species pairs well
            }
            else if (Species.StartsWith("Hybrid") || other.Species.StartsWith("Hybrid"))
            {
                compatibility += 0.3; // Hybrids have decent compatibility
            }
            else
            {
                compatibility += 0.1; // Different species have low compatibility
            }
            
            // Genetic diversity factor
            double geneticDiversity = 0;
            int traitCount = 0;
            
            foreach (GeneticTrait trait in Genes.Keys)
            {
                if (other.Genes.TryGetValue(trait, out int otherValue))
                {
                    // The more different the gene values, the more diverse
                    geneticDiversity += Math.Abs(Genes[trait] - otherValue) / 100.0;
                    traitCount++;
                }
            }
            
            // Average genetic diversity (0-1)
            if (traitCount > 0)
            {
                geneticDiversity /= traitCount;
                
                // Moderate diversity (0.3-0.7) is best for breeding
                if (geneticDiversity >= 0.3 && geneticDiversity <= 0.7)
                {
                    compatibility += 0.4;
                }
                else
                {
                    compatibility += 0.2; // Too similar or too different
                }
            }
            
            // Special traits compatibility
            int sharedTraits = 0;
            
            foreach (SpecialTrait trait in SpecialTraits)
            {
                if (other.SpecialTraits.Contains(trait))
                {
                    sharedTraits++;
                }
            }
            
            double traitCompatibility = sharedTraits > 0 ? 
                Math.Min(0.1 + (sharedTraits * 0.05), 0.3) : 0.1;
            
            compatibility += traitCompatibility;
            
            return Math.Min(compatibility, 1.0); // Ensure it's in the 0-1 range
        }
        
        /// <summary>
        /// Get a description of the bonsai's genetic profile
        /// </summary>
        public string GetGeneticDescription()
        {
            string description = $"Species: {Species}\n";
            description += $"Dominant Style: {DominantStyle}\n";
            description += $"Rarity: {Rarity}\n\n";
            
            description += "Genetic Traits:\n";
            
            // Find the top three most extreme traits
            var sortedGenes = Genes.OrderByDescending(g => Math.Abs(g.Value - 50)).Take(3);
            
            foreach (var gene in sortedGenes)
            {
                string traitDescription = gene.Value switch
                {
                    >= 90 => "Exceptional",
                    >= 75 => "Strong",
                    >= 60 => "Above Average",
                    >= 40 => "Average",
                    >= 25 => "Below Average",
                    >= 10 => "Weak",
                    _ => "Very Weak"
                };
                
                description += $"• {gene.Key}: {traitDescription} ({gene.Value}/100)\n";
            }
            
            if (SpecialTraits.Count > 0)
            {
                description += "\nSpecial Traits:\n";
                foreach (SpecialTrait trait in SpecialTraits)
                {
                    description += $"• {trait}\n";
                }
            }
            
            return description;
        }
        
        #endregion
    }
    
    #region Genetics Enums
    
    /// <summary>
    /// Genetic traits that can be inherited
    /// </summary>
    public enum GeneticTrait
    {
        // Physical traits
        Height,
        BranchDensity,
        LeafSize,
        TrunkThickness,
        RootStrength,
        LeafColor,
        BarkTexture,
        
        // Growth traits
        GrowthRate,
        Longevity,
        FloweringPotential,
        FruitingPotential,
        
        // Adaptability traits
        DroughtTolerance,
        ColdTolerance,
        DiseaseResistance,
        PestResistance,
        
        // Aesthetic traits
        Symmetry,
        Lean,
        Drooping,
        Straightness,
        Flexibility,
        Resilience
    }
    
    /// <summary>
    /// Special traits that are rare and can be inherited
    /// </summary>
    public enum SpecialTrait
    {
        RedLeaves,
        VarlegetedLeaves,
        DwarfGrowth,
        GiantGrowth,
        EarlyFlowering,
        AbnormalBranching,
        DoubleTrunk,
        GoldenFoliage,
        WeepingForm,
        AromaticWood,
        IridescentBark,
        ExoticFruits
    }
    
    /// <summary>
    /// Rarity levels for bonsai specimens
    /// </summary>
    public enum BonsaiRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
    
    #endregion
}