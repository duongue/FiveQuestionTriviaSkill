using System;
using Amazon.Lambda.Core;
using Slight.Alexa.Framework.Models.Responses;
using Slight.Alexa.Framework.Models.Requests;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace FiveQuestionSkill
{
    public class Function
    {
        // game session variables
        private const int NumberOfQuestion = 5;
        private int pickedAge = 0;
        private int pickedCategory = 0;
        private int questionIndex = 0;
        private int score = 0;
        private List<Question> questions = new List<Question>();
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            Response response;
            IOutputSpeech innerResponse = null;
            var log = context.Logger;
            bool endSession = false;
            string output;

            if (input.GetRequestType() == typeof(Slight.Alexa.Framework.Models.Requests.RequestTypes.ILaunchRequest)
                || input.Request.Intent.Name == "AMAZON.StartOverIntent")
            {
                // default launch request, let's just let them know what you can do
                //log.LogLine($"Default LaunchRequest made");
                clearSessionValues();
                output = $"Welcome to five question trivia for kids. First, tell me your age. Say 'I am', and then your age.";
            }
            else if (input.GetRequestType() == typeof(Slight.Alexa.Framework.Models.Requests.RequestTypes.IIntentRequest))
            {
                switch (input.Request.Intent.Name)
                {
                    case "PickAgeIntent":
                        List<Level> levels = new List<Level>();
                        levels = loadLevels();
                        var selectedAge = input.Request.Intent.Slots["Age"].Value;
                        var findAge = levels.FirstOrDefault(s => s.Description.Contains(selectedAge));
                        if (findAge != null)
                        {
                            pickedAge = findAge.LevelId;
                        }
                        else
                        {
                            pickedAge = 1;
                        }
                        output = $"Great! We can play sevearl categories such as general, math, and science. To pick category, say 'lets play', then the category name.";
                        break;
                    case "PickCategoryIntent":                
                        List<Category> categories = new List<Category>();
                        loadGameState(input);
                        categories = loadCategories();
                        var selectedCategory = input.Request.Intent.Slots["Category"].Value;
                        var findCategory = categories.FirstOrDefault(s => s.Description.ToLower().Contains(selectedCategory.ToLower()));
                        if (findCategory != null)
                        {
                            pickedCategory = findCategory.CategoryId;
                        }
                        else
                        {
                            pickedCategory = 1;
                        }

                        //log.LogLine($"inputs are: " + pickedAge + " " + pickedCategory);
                        questions = loadQuestions(pickedAge, pickedCategory, NumberOfQuestion);
                        output = "";
                        output += $" You have selected the " + selectedCategory + " category. Let's begin! To answer a question, remember to say 'my answer is', then your answer.";
                        output += $" Question " + (questionIndex + 1) + ": ";
                        log.LogLine("questionIndex is " + questionIndex);
                        log.LogLine("questions is " + questions.Count);
                        output += questions[questionIndex].Text + '?';
                        //log.LogLine("questionIndex is " + questionIndex);
                        break;
                    case "AnswerIntent":                
                        // game session variables
                        output = "";
                        string answer = input.Request.Intent.Slots["Answer"].Value;
                        loadGameState(input);
                        bool answeredCorrectly = answer.ToLower().Contains(questions[questionIndex].Answer.ToLower());
                        var encouragement = getEncouragement(answeredCorrectly);
                        if (answeredCorrectly)
                        {
                            score += 1;
                            output += $" That is correct! " + encouragement.Text + ".";
                        }
                        else
                        {
                            output += encouragement.Text + $". The correct answer is " + questions[questionIndex].Answer + ".";
                        }
                        output += $" Your score is " + score + " out of " + (questionIndex + 1) + ".";

                        if (questionIndex < NumberOfQuestion - 1)
                        {
                            // continute game
                            questionIndex++;
                            output += $" Question " + (questionIndex + 1) + ": ";
                            output += questions[questionIndex].Text + '?';                        
                        }
                        else
                        {
                            output += $" That is the end of our game. Thanks for playing.";
                            endSession = true;
                            clearSessionValues();
                        }
                        break;
                    case "DontKnowIntent":
                        output = "";
                        loadGameState(input);
                        output = "That is okay. The correct answer is " + questions[questionIndex].Answer + ".";
                        output += $" Your score is " + score + " out of " + (questionIndex + 1) + ".";

                        if (questionIndex < NumberOfQuestion - 1)
                        {
                            // continute game
                            questionIndex++;
                            output += $" Question " + (questionIndex + 1) + ": ";
                            output += questions[questionIndex].Text + '?';
                        }
                        else
                        {
                            output += $" That is the end of our game. Thanks for playing.";
                            endSession = true;
                            clearSessionValues();
                        }
                        break;
                    case "AMAZON.RepeatIntent":
                        output = "";
                        output += $"Question " + (questionIndex + 1) + ": ";
                        output += questions[questionIndex].Text + '?';
                        break;
                    case "AMAZON.HelpIntent":
                        output = "To say age, say 'I am', then your age. To pick category, say 'play', then category name. To answer, say 'answer is', then your actual answer";
                        break;
                    case "AMAZON.StopIntent":
                        output = $"Thanks for playing. Goodbye!";
                        endSession = true;
                        clearSessionValues();
                        break;
                    default:
                        output = "To answer, say 'answer is', then your actual answer";
                        break;
                }
                

            }
            else if (input.GetRequestType() == typeof(Slight.Alexa.Framework.Models.Requests.RequestTypes.ISessionEndedRequest))
            {
                // intent request, process the intent
                //log.LogLine($"End Intent Request Made");
                output = $". Thanks for playing. Goodbye!";
                endSession = true;
                clearSessionValues();
            }
            else
            {
                output = "To say age, say 'I am', then your age. To pick category, say 'play', then category name. To answer, say 'answer is', then your actual answer";
            }

            innerResponse = new PlainTextOutputSpeech();
            (innerResponse as PlainTextOutputSpeech).Text = output;
            response = new Response();
            response.ShouldEndSession = endSession;
            response.OutputSpeech = innerResponse;
            SkillResponse skillResponse = new SkillResponse();            
            skillResponse.Response = response;
            skillResponse.Version = "1.0";
            skillResponse.SessionAttributes = new System.Collections.Generic.Dictionary<string, object>();
            skillResponse.SessionAttributes["pickedAge"] = pickedAge;
            skillResponse.SessionAttributes["pickedCategory"] = pickedCategory;
            skillResponse.SessionAttributes["questionIndex"] = questionIndex;
            skillResponse.SessionAttributes["score"] = score;
            skillResponse.SessionAttributes["questions"] = JsonConvert.SerializeObject(questions);

            return skillResponse;
        }

        private void clearSessionValues()
        {
            pickedAge = 0;
            pickedCategory = 0;
            questionIndex = 0;
            score = 0;
            questions = new List<Question>();
    }

        private void loadGameState(SkillRequest input)
        {
            pickedAge = Convert.ToInt32(input.Session.Attributes["pickedAge"]);
            pickedCategory = Convert.ToInt32(input.Session.Attributes["pickedCategory"]);
            questionIndex = Convert.ToInt32(input.Session.Attributes["questionIndex"]);
            score = Convert.ToInt32(input.Session.Attributes["score"]);
            questions = JsonConvert.DeserializeObject<List<Question>>(input.Session.Attributes["questions"].ToString());
    }

        private Encouragement getEncouragement(bool answeredCorrectly)
        {
                return JsonConvert.DeserializeObject<Encouragement>(client.GetStringAsync("https://alexakidskill.azurewebsites.net/api/Game/GetEncouragement?answeredCorrectly=" + answeredCorrectly).Result);
           
        }

        private List<Question> loadQuestions(int pickedAge, int pickedCategory, int numberOfQuestion)
        {
             var questionPost = new QuestionsPost();
             questionPost.LevelId = pickedAge;
             questionPost.CategoryId = pickedCategory;
             questionPost.NumberOfQuestions = NumberOfQuestion;
             string sr = JsonConvert.SerializeObject(questionPost);
             var content = new StringContent(sr, Encoding.UTF8, "application/json");

             var response = client.PostAsync("https://alexakidskill.azurewebsites.net/api/Game/GetQuestions", content).Result;
             string responseInString = response.Content.ReadAsStringAsync().Result;
             return JsonConvert.DeserializeObject<List<Question>>(responseInString);
        }

        private List<Level> loadLevels()
        {
             return JsonConvert.DeserializeObject<List<Level>>(client.GetStringAsync("https://alexakidskill.azurewebsites.net/api/Game/GetLevels").Result);               
        }

        private List<Category> loadCategories()
        {           
             return JsonConvert.DeserializeObject<List<Category>>(client.GetStringAsync("https://alexakidskill.azurewebsites.net/api/Game/GetCategories").Result);
        }

        private void loadGameSessionVariables()
        {
            
        }
    }

    public class Level
    {
        public int LevelId { get; set; }
        public string Description { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string Description { get; set; }
    }

    public class Encouragement
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool SaysOnCorrectAnswer { get; set; }
    }

    public class Question
    {
        public int QuestionId { get; set; }
        public int LevelId { get; set; }
        public int CategoryId { get; set; }
        public string Text { get; set; }
        public string Answer { get; set; }
        public string AdditionalInfo { get; set; }
        public string Choices { get; set; }
    }

    public class QuestionsPost
    {
        public int LevelId { get; set; }
        public int CategoryId { get; set; }
        public int NumberOfQuestions { get; set; }
    }

    public class AnswerPost
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; }
    }
}
