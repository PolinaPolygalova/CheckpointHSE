using Microsoft.AspNetCore.Http;
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
        const string RECOGNITION_MODEL3 = RecognitionModel.Recognition03;

        public static Dictionary<string, string> EmployeesNames { get; set; }

        static string personGroupId = Guid.NewGuid().ToString();

        private static IFaceClient faceClient;

        public static bool IsTrained { get; set; }

        public async static void Initialize(IServiceProvider serviceProvider)
        {
            Dictionary<string, string> employees;

            IsTrained = false;

            using (var context = new EmployeesContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<EmployeesContext>>()))
            {
                employees = await context.Employees.ToDictionaryAsync(k => k.Guid, v => v.Surname + " " + v.Name + " " + v.Patronymic);
            }

            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            faceClient = client;

            // Identify - recognize a face(s) in a person group (a person group is created in this example).
            IdentifyInPersonGroup(client, RECOGNITION_MODEL3, employees).Wait();
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
            IsTrained = true;

        }
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFormFile file)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            //IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03);
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(file.OpenReadStream(), recognitionModel: RECOGNITION_MODEL3, detectionModel: DetectionModel.Detection03);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{file.FileName}`");
            return detectedFaces.ToList();
        }

        public static async Task<string> IdentifyFacesAsync(IFormFile file)
        {
            string identifiedPersonName = string.Empty;

            List<Guid> sourceFaceIds = new List<Guid>();
            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(file);

            if (detectedFaces is null || detectedFaces.Count == 0)
            {
                return identifiedPersonName;
            }

            // Add detected faceId to sourceFaceIds.
            foreach (var detectedFace in detectedFaces)
            { 
                sourceFaceIds.Add(detectedFace.FaceId.Value); 
            }

            // Identify the faces in a person group. 
            var identifyResults = await faceClient.Face.IdentifyAsync(sourceFaceIds, personGroupId);

            if (identifyResults != null && identifyResults.Count == 1)
            {
                if (identifyResults[0].Candidates != null && identifyResults[0].Candidates.Count > 0)
                {
                    Person person = await faceClient.PersonGroupPerson.GetAsync(personGroupId, identifyResults[0].Candidates[0].PersonId);
                    Console.WriteLine($"Person '{person.Name}' is identified for face in: {file.FileName} - {identifyResults[0].FaceId}," +
                        $" confidence: {identifyResults[0].Candidates[0].Confidence}.");
                    identifiedPersonName = person.Name;
                }
            }
            
            return identifiedPersonName;
        }
    }    
}
