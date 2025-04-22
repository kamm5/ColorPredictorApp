using Microsoft.ML.Data;

namespace ColorPredictorApp
{
    public partial class MainWindow
    {
        public class PredictionResult
        {
            [ColumnName("Score")]
            public float Score { get; set; }
        }
    }
}