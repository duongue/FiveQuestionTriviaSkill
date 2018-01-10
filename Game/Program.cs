using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Game
{
    class Program
    {
        static readonly HttpClient client = new HttpClient();
        static void Main(string[] args)
        {
            List<Level> levels = new List<Level>();
            List<Category> categories = new List<Category>();
            List<Question> questions = new List<Question>();
            const int NumberOfQuestion = 5;
            int pickedAge = 0;
            int pickedCategory = 0;
            int questionIndex = 0;
            int score = 0;
            levels = JsonConvert.DeserializeObject<List<Level>>(client.GetStringAsync("http://alexakidskill.azurewebsites.net/api/Game/GetLevels").Result);
            categories = JsonConvert.DeserializeObject<List<Category>>(client.GetStringAsync("http://alexakidskill.azurewebsites.net/api/Game/GetCategories").Result);
            Console.WriteLine("Please choose an age group: ");
            string chosenLevel = Console.ReadLine();

            var findAge = levels.FirstOrDefault(s => s.Description.Contains(chosenLevel));
            if (findAge != null)
            {
                pickedAge = findAge.LevelId;
            }
            else
            {
                pickedAge = 1;
            }

            Console.WriteLine("Please pick a category: ");
            string chosenCategory = Console.ReadLine();

            var findCategory = categories.FirstOrDefault(s => s.Description.ToLower().Contains(chosenCategory.ToLower()));
            if (findCategory != null)
            {
                pickedCategory = findCategory.CategoryId;
            }else
            {
                pickedCategory = 1;
            }

                var questionPost = new QuestionsPost();
                questionPost.LevelId = pickedAge;
                questionPost.CategoryId = pickedCategory;
                questionPost.NumberOfQuestions = NumberOfQuestion;
                string sr = JsonConvert.SerializeObject(questionPost);
                var content = new StringContent(sr, Encoding.UTF8, "application/json");

                var response = client.PostAsync("http://alexakidskill.azurewebsites.net/api/Game/GetQuestions", content).Result;
                string responseInString = response.Content.ReadAsStringAsync().Result;
                questions = JsonConvert.DeserializeObject<List<Question>>(responseInString);

            while (questionIndex < NumberOfQuestion)
            {
                Console.WriteLine(questions[questionIndex].Text + '?');
                string answer = Console.ReadLine();
                bool answeredCorrectly = answer.ToLower().Contains(questions[questionIndex].Answer.ToLower());
                var encouragement = getEncouragement(answeredCorrectly);
                if (answeredCorrectly)
                {
                    score += 1;
                    Console.WriteLine("That is correct! " + encouragement.Text);
                }
                else
                {
                    Console.WriteLine(encouragement.Text + ". The correct answer is " + questions[questionIndex].Answer);
                }
                Console.WriteLine("Your score is " + score + " out of " + (questionIndex + 1));
                questionIndex++;
            }
        }

        static Encouragement getEncouragement(bool answeredCorrectly)
        {
                return JsonConvert.DeserializeObject<Encouragement>(client.GetStringAsync("http://alexakidskill.azurewebsites.net/api/Game/GetEncouragement?answeredCorrectly=" + answeredCorrectly).Result);

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
