using System;

namespace ImageDetails
{
    public class ImageMetadata
    {        
         public string id
        {
            get;
            set;
        }
        
        public string uploadUserName
        {
            get;
            set;
        }

        public DateTime timestamp
        {
            get;
            set;
        }

        public double geoLatCoordinate
        {
            get;
            set;
        }

        public double geoLongCoordinate
        {
            get;
            set;
        }

        public double probability
        {
            get;
            set;
        }

        public string tagName
        {
            get;
            set;
        }

        public string issueType
        {
            get;
            set;
        }

        public string issueDescription
        {
            get;
            set;
        }

        public bool isValidatedIssue
        {
            get;
            set;
        }

    }
}
