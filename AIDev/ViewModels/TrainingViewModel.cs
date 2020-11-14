using System.Collections.Generic;
using OxyPlot;

//xmlns:oxy="http://oxyplot.org/wpf"
//xmlns:oxy="http://oxyplot.codeplex.com" 
namespace AIDev.ViewModels
{
    // Change to "TrainingModel"?
    public class TrainingViewModel
    {
        public TrainingViewModel()
        {
            //MyModel = new PlotModel { Title = "Example 1" };
            //MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));

            Title = "Error";
            Points = new List<DataPoint>
            {
                new DataPoint(1, .9),
                new DataPoint(2, .7),
                new DataPoint(3, .6),
                new DataPoint(4, .56),
                new DataPoint(5, .52),
                new DataPoint(6, .45),
                new DataPoint(7, .42),
                new DataPoint(8, .40),
                new DataPoint(9, .39),
                new DataPoint(10, .38)
            };
        }

        public string Title { get; set; }
        public static IList<DataPoint> Points { get; set; }
    }
}
