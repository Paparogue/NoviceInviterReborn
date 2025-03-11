using Microsoft.ML.Data;

namespace NoviceInviterReborn
{
    public class NameData
    {
        [LoadColumn(0)]
        public string? Name { get; set; }

        [LoadColumn(1)]
        public float IsFiltered { get; set; }
    }

    public class NamePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
    }
}
