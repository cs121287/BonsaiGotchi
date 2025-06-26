using System;
using System.Collections.Generic;

namespace BonsaiGotchi
{
    /// <summary>
    /// Extensions to the BonsaiPet class for Phase 3 features
    /// </summary>
    public partial class BonsaiPet
    {
        #region Phase 3 Properties
        
        // Activity timing bonuses/penalties
        public bool MorningActivityBonus { get; set; }
        public bool EveningActivityBonus { get; set; }
        public bool NightActivityPenalty { get; set; }
        
        // Additional information about the bonsai's origin
        public bool IsFromBreeding { get; set; }
        public Guid? ParentId1 { get; set; }
        public Guid? ParentId2 { get; set; }
        
        #endregion
        
        #region Phase 3 Methods
        
        /// <summary>
        /// Get bonus factor for care actions based on time of day
        /// </summary>
        public double GetTimeOfDayBonusFactor()
        {
            if (MorningActivityBonus)
                return 1.2; // 20% bonus
            
            if (EveningActivityBonus)
                return 1.1; // 10% bonus
            
            if (NightActivityPenalty)
                return 0.8; // 20% penalty
            
            return 1.0; // No bonus or penalty
        }
        
        /// <summary>
        /// Enhanced water method that considers time of day
        /// </summary>
        public void EnhancedWater()
        {
            double bonusFactor = GetTimeOfDayBonusFactor();
            
            // Store initial hydration for comparison
            double initialHydration = Hydration;
            
            // Call base watering method
            Water();
            
            // Apply time of day bonus or penalty
            double hydrationChange = Hydration - initialHydration;
            double adjustedChange = hydrationChange * bonusFactor - hydrationChange;
            
            Hydration = Math.Max(0, Math.Min(100, Hydration + adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (bonusFactor > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Watering Bonus", 
                    $"Watering at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (bonusFactor < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Watering Penalty", 
                    $"Watering at night is less effective. Try watering in the morning for best results.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Enhanced pruning method that considers time of day
        /// </summary>
        public void EnhancedPrune()
        {
            double bonusFactor = GetTimeOfDayBonusFactor();
            
            // Store initial values for comparison
            double initialPruningQuality = PruningQuality;
            
            // Call base pruning method
            Prune();
            
            // Apply time of day bonus or penalty
            double pruningChange = PruningQuality - initialPruningQuality;
            double adjustedChange = pruningChange * bonusFactor - pruningChange;
            
            PruningQuality = Math.Max(0, Math.Min(100, PruningQuality + adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (bonusFactor > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Pruning Bonus", 
                    $"Pruning at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (bonusFactor < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Pruning Penalty", 
                    $"Pruning at night is less effective. Try pruning in the evening for best results.",
                    NotificationSeverity.Warning));
            }
        }
        
        /// <summary>
        /// Enhanced feeding method that considers time of day
        /// </summary>
        public void EnhancedFeed()
        {
            double bonusFactor = GetTimeOfDayBonusFactor();
            
            // Store initial values for comparison
            double initialHunger = Hunger;
            
            // Call base feeding method
            Feed();
            
            // Apply time of day bonus or penalty
            double hungerChange = initialHunger - Hunger; // Hunger decreases when feeding
            double adjustedChange = hungerChange * bonusFactor - hungerChange;
            
            Hunger = Math.Max(0, Math.Min(100, Hunger - adjustedChange));
            
            // Add notification if there was a significant bonus or penalty
            if (bonusFactor > 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Feeding Bonus", 
                    $"Feeding at this time of day is especially effective!",
                    NotificationSeverity.Information));
            }
            else if (bonusFactor < 1.0)
            {
                AddNotification(new BonsaiNotification(
                    "Feeding Penalty", 
                    $"Feeding at night is less effective. Try feeding during the day for best results.",
                    NotificationSeverity.Warning));
            }
        }
        
        #endregion
    }
}