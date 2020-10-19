using System;

namespace PredictionDetails
{
    public class Prediction
    {
        public double probability
        {
            get;
            set;
        }

        public Guid tagId
        {
            get;
            set;
        }

        public String tagName
        {
            get;
            set;
        }
    }
}
