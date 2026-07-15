using System;
using Newtonsoft.Json;

namespace ONIUtilityTweaks.CarePackages
{
    public sealed class CarePackageDefinition
    {
        [JsonProperty("ID")]
        public string Id { get; set; }

        [JsonProperty("amount")]
        public float Amount { get; set; }

        [JsonProperty("onlyAfterCycle", NullValueHandling = NullValueHandling.Ignore)]
        public int? OnlyAfterCycle { get; set; }

        [JsonProperty("onlyUntilCycle", NullValueHandling = NullValueHandling.Ignore)]
        public int? OnlyUntilCycle { get; set; }

        public CarePackageDefinition()
        {
        }

        public CarePackageDefinition(
            string id,
            float amount,
            int? onlyAfterCycle = null,
            int? onlyUntilCycle = null)
        {
            Id = id;
            Amount = amount;
            OnlyAfterCycle = onlyAfterCycle;
            OnlyUntilCycle = onlyUntilCycle;
        }

        internal CarePackageInfo ToInfo(int multiplier)
        {
            float quantity = (float)Math.Max(Math.Round(Amount * multiplier, 0), 1.0);
            if (CarePackageDefaults.IsPrimoGarb(Id))
            {
                return new CarePackageInfo(
                    Id, quantity, () => IsAvailable(), Immigration.FACADE_SELECT_RANDOM);
            }
            return new CarePackageInfo(Id, quantity, () => IsAvailable());
        }

        private bool IsAvailable()
        {
            int cycle = GameClock.Instance.GetCycle();
            return cycle >= (OnlyAfterCycle ?? -1) &&
                cycle <= (OnlyUntilCycle ?? int.MaxValue);
        }
    }
}
