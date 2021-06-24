using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CheckpointHSEWebServer
{
    public static class EmployeesInfo
    {
        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        const string SUBSCRIPTION_KEY = "aacf702741784695807ec7d9d56b4e51";
        const string ENDPOINT = "https://checkpointhse.cognitiveservices.azure.com/";

        public static Dictionary<string, string> EmployeesNames { get; set; }

        static string personGroupId = Guid.NewGuid().ToString();

        public async static void Initialize(IServiceProvider serviceProvider)
        {
            Dictionary<string, string> employees;

            using (var context = new EmployeesContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<EmployeesContext>>()))
            {
                employees = await context.Employees.ToDictionaryAsync(k => k.Guid, v => v.Surname + " " + v.Name + " " + v.Patronymic);
            }

            const string RECOGNITION_MODEL3 = RecognitionModel.Recognition03;

            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

            //// Detect - get features from faces.
            //DetectFaceExtract(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// Find Similar - find a similar face from a list of faces.
            //FindSimilar(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();
            //// Verify - compare two images if the same person or not.
            //Verify(client, IMAGE_BASE_URL, RECOGNITION_MODEL3).Wait();

            // Identify - recognize a face(s) in a person group (a person group is created in this example).
            IdentifyInPersonGroup(client, RECOGNITION_MODEL3, employees).Wait();
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

        public static async Task IdentifyInPersonGroup(IFaceClient client, string recognitionModel, Dictionary<string, string> employees)
        {
            Console.WriteLine("========IDENTIFY FACES========");
            Console.WriteLine();

            EmployeesNames = new Dictionary<string, string>(employees.Count);

            // A group photo that includes some of the persons you seek to identify from your dictionary.
            string sourceImageFileName = "identification1.jpg";
            // Create a person group. 
            Console.WriteLine($"Create a person group ({personGroupId}).");
            await client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);
            // The similar faces will be grouped into a single person group person.
            foreach (var employee in employees)
            {
                // Limit TPS
                await Task.Delay(250);
                Person person = await client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: employee.Value);
                Console.WriteLine($"Create a person group person '{employee.Value}'.");

                var directoryPath = Path.Combine(@"C:\CheckpointHSE", employee.Key);
                if (!Directory.Exists(directoryPath))
                {
                    //чо делать если нет папки с картинками
                }
                
                EmployeesNames.Add(person.PersonId.ToString(), employee.Value);
                // Add face to the person group person.
                foreach (var filePath in Directory.GetFiles(directoryPath))
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        Console.WriteLine($"Add face to the person group person({employee.Value}) from image `{filePath}`");
                        PersistedFace face = await client.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, fileStream);
                    }
                }
            }
            // Start to train the person group.
            Console.WriteLine();
            Console.WriteLine($"Train person group {personGroupId}.");
            await client.PersonGroup.TrainAsync(personGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(personGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
            Console.WriteLine();

        }
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(url)}`");
            return detectedFaces.ToList();
        }

        //public static async Task<List<string>> IdentifyFacesAsync(IFaceClient client, string recognitionModel)
        //{
        //    List<string> identifiedPersonNames = new List<string>();

        //    List<Guid?> sourceFaceIds = new List<Guid?>();
        //    // Detect faces from source image url.
        //    List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognitionModel);

        //    // Add detected faceId to sourceFaceIds.
        //    foreach (var detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

        //    // Identify the faces in a person group. 
        //    var identifyResults = await client.Face.IdentifyAsync((IList<Guid>)sourceFaceIds, personGroupId);

        //    foreach (var identifyResult in identifyResults)
        //    {
        //        Person person = await client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
        //        Console.WriteLine($"Person '{person.Name}' is identified for face in: {sourceImageFileName} - {identifyResult.FaceId}," +
        //            $" confidence: {identifyResult.Candidates[0].Confidence}.");
        //        identifiedPersonNames.Add(person.Name);
        //    }
        //    Console.WriteLine();

        //    return identifiedPersonNames;
        //}
    }    
}
