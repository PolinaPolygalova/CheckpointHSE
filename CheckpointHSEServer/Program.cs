// <snippet_using>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
// </snippet_using>

/**
 * FACE QUICKSTART
 * 
 * This quickstart includes the following examples for Face:
 *  - Detect Faces
 *  - Find Similar
 *  - Identify faces (and person group operations)
 *  - Large Person Group 
 *  - Group Faces
 *  - FaceList
 *  - Large FaceList
 * 
 * Prerequisites:
 *  - Visual Studio 2019 (or 2017, but this is app uses .NETCore, not .NET Framework)
 *  - NuGet libraries:
 *    Microsoft.Azure.CognitiveServices.Vision.Face
 *    
 * How to run:
 *  - Create a new C# Console app in Visual Studio 2019.
 *  - Copy/paste the Program.cs file in the Github quickstart into your own Program.cs file. 
 *  
 * Dependencies within the samples: 
 *  - Authenticate produces a client that's used by all samples.
 *  - Detect Faces is a helper function that is used by several other samples. 
 *   
 * References:
 *  - Face Documentation: https://docs.microsoft.com/en-us/azure/cognitive-services/face/
 *  - .NET SDK: https://docs.microsoft.com/en-us/dotnet/api/overview/azure/cognitiveservices/client/face?view=azure-dotnet
 *  - API Reference: https://docs.microsoft.com/en-us/azure/cognitive-services/face/apireference
 */

namespace CheckpointHSE
{
    public class Keys
    {

        public const string FaceKey = "f19533d0a5b3463bb6c4e01843902d7d";
        public const string VisualizationKey = "7f27373f75a547b5bbdb8abee9644468";
        public const string FaceEndpoint = "https://checkpointhsetry.cognitiveservices.azure.com/";
        public const string VisualizationEndpoint = "https://checkpointhsecomputervision.cognitiveservices.azure.com/";
        public const string IMAGE_BASE_URL = @"C:\Users\Екатерина\source\repos\CheckpointHSE\CheckpointHSE\Images\";
    }

    class Program 
    { 
        static IFaceClient client = Authenticate(Keys.FaceEndpoint, Keys.FaceKey);

        static async Task Main(string[] args) 
        { 
            string personGroupID = DataBase.CreatePersonGroupAsync(client); 

            // Групповое фото, включающее несколько людей из БД
            string sourceImageFileName = "identification1.jpeg";

            List<Guid?> sourceFaceIds = new List<Guid?>();

            // Определяет лица на групповом фото
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, Keys.IMAGE_BASE_URL + sourceImageFileName, RecognitionModel.Recognition03);

            // Добавляет лица в sourceFaceIds.
            foreach (var detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

            // Определяет людей на фото
            var identifyResults = await client.Face.IdentifyAsync(sourceFaceIds, DataBase.personGroupId);

            foreach (var identifyResult in identifyResults)
            {
                if (identifyResult.Candidates.Count > 0)
                {
                    Person person = await client.PersonGroupPerson.GetAsync(DataBase.personGroupId, identifyResult.Candidates[0].PersonId);
                    Console.WriteLine($"Person '{person.Name}' is identified for face in: {sourceImageFileName} - {identifyResult.FaceId}," +
                        $" confidence: {identifyResult.Candidates[0].Confidence}.");
                }
                else { }                
            }
        }

        // Определяет лица на фото
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.

            FileStream stream = new FileStream(url, FileMode.Open);
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection02);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{url}`");
            return detectedFaces.ToList();
        }


        // Авторизация
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
    }    

    public class DataBase
    {
        // Создает ID для базы данных
        public static string personGroupId = Guid.NewGuid().ToString();

        // Создает БД из фотографий, хранящихся на устройстве
        private static async Task IdentifyInPersonGroup(IFaceClient client, string url, string recognitionModel)
        {
            Dictionary<string, string[]> personDictionary =
                new Dictionary<string, string[]>
                    { { "Ekaterina S.", new[] { "ES1.jpg", "ES2.jpg" } },
                    { "Polina P.", new[] { "PP1.jpg", "PP2.jpg" } },
                    { "Polina B.", new[] { "PB1.jpg", "PB2.jpg" } },
                    };


            Console.WriteLine($"Создание person group ({personGroupId}).");
            client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel).Wait();

            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);
                Person person = await client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
                Console.WriteLine($"Create a person group person '{groupedFace}'.");

                // Добавление лица в person group person.
                foreach (var similarImage in personDictionary[groupedFace])
                {
                    FileStream stream = new FileStream(url + similarImage, FileMode.Open);
                    PersistedFace face = await client.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId,
                        stream, similarImage);
                }
            }

            // Тренировка person group
            await client.PersonGroup.TrainAsync(personGroupId);

            // Ожидание окончания тренировки
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(personGroupId);
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
        }

        // Удаление person group
        public static async Task DeletePersonGroup(IFaceClient client, string personGroupId)
        {
            await client.PersonGroup.DeleteAsync(personGroupId);
            Console.WriteLine($"Deleted the person group {personGroupId}.");
        }

        // Вызывает создание БД
        public static string CreatePersonGroupAsync(IFaceClient client)
        {
            IdentifyInPersonGroup(client, Keys.IMAGE_BASE_URL, RecognitionModel.Recognition03).Wait();
            return personGroupId;
        }
    }
}