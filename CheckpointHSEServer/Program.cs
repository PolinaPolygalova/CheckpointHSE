using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace CheckpointHSEServer
{
    class Program
    {
        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        const string SUBSCRIPTION_KEY = "22c61a1b0d3e44cfaa547e2c234aba72";
        const string ENDPOINT = "https://westcentralus.api.cognitive.microsoft.com/face";

        static void Main(string[] args)
        {
            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

            //// Detect - get features from faces.
            //DetectFaceExtract(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// Find Similar - find a similar face from a list of faces.
            //FindSimilar(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// Verify - compare two images if the same person or not.
            //Verify(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();

            //// Identify - recognize a face(s) in a person group (a person group is created in this example).
            //IdentifyInPersonGroup(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// LargePersonGroup - create, then get data.
            //LargePersonGroup(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// Group faces - automatically group similar faces.
            //Group(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// FaceList - create a face list, then get data
        }

        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
    }
}
