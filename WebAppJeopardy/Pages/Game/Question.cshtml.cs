using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net.Http;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json.Linq;


namespace WebAppJeopardy.Pages.Game
{
    public class QuestionModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private const string OPENAI_ENDPOINT = "https://api.openai.com/v1/chat/completions"; //endpoint

        private string OPENAI_API_KEY = "";
        private readonly HttpClient _client;
        private IConfiguration _configuration { get; }

        public class JeopardyQuestionsAndAnswer
        {

            public string? QuestionText { get; set; } = "";
            public IDictionary<char, string> QuestionsOptions { get; set; } = new Dictionary<char, string>();

            public char? Answer { get; set; }
            public string? AnswerText { get; set; }

            public char UserSelectedAnswer { get; set; }
            public string? UserSelectedAnswerText { get; set; }
            public bool UserAnswerIsCorrect { get; set; }

        }

        public JeopardyQuestionsAndAnswer JeopartyQA { get; set; }

        public QuestionModel(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _client = new HttpClient();
            OPENAI_API_KEY = _configuration["API:APIKEY"];
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OPENAI_API_KEY);
        }



        [BindProperty]
        public char UserSelectedAnswer { get; set; }


        public IActionResult OnPostSubmit(char userSelectedAnswer, string jeopartyQAData)
        {
            JeopartyQA = JsonConvert.DeserializeObject<JeopardyQuestionsAndAnswer>(jeopartyQAData);
            JeopartyQA.UserSelectedAnswer = userSelectedAnswer;


            if (JeopartyQA.QuestionsOptions.TryGetValue(userSelectedAnswer, out string value))
            {
                JeopartyQA.UserSelectedAnswerText = value;
            }

            JeopartyQA.UserAnswerIsCorrect = JeopartyQA.Answer == userSelectedAnswer;



            TempData["Jeoparty"] = JsonConvert.SerializeObject(JeopartyQA);


            return RedirectToPage("/Game/Result");


        }


        public async Task<IActionResult> OnGetAsync()
        {
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OPENAI_API_KEY);

            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {

                                  new
            {
                role = "system",
                content = "Create a question that about movies with four multiple-choice answers labeled [A], [B], [C], and [D]. " +
                       "Follow the following format to provide question and answer: " +
                       "What is the capital of USA? \n\n [A] Washington DC, [B] New York, [C] Los Angeles, [D] Chicago. Correct Answer: [A],Washington DC."
            }

                        }
            };



            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");


            var response = await client.PostAsync(OPENAI_ENDPOINT, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                string filePath = @"C:\Log\JsonResponse.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(responseString);
                }


                JeopartyQA = new JeopardyQuestionsAndAnswer();
                ParseQuestion(responseString, JeopartyQA);

                return Page();
            }
            else
            {
                return NotFound();
            }

        }


        private void ParseQuestion(string json, JeopardyQuestionsAndAnswer Jeopartyqa)
        {
            var jsonData = JObject.Parse(json);
            var content = jsonData["choices"][0]["message"]["content"].ToString();

            // Split into question and the rest
            int firstOptionIndex = content.IndexOf("[A]");
            Jeopartyqa.QuestionText = content.Substring(0, firstOptionIndex).Trim();

            // Remaining content including options
            string remainingContent = content.Substring(firstOptionIndex);

            // Markers for options and correct answer
            string[] markers = new string[] { "[A]", "[B]", "[C]", "[D]", "Correct Answer:" };

            // Process each option
            for (int i = 0; i < markers.Length - 1; i++)
            {
                int startIndex = remainingContent.IndexOf(markers[i]) + 3; // Length of marker like '[A]'
                int endIndex = remainingContent.IndexOf(markers[i + 1]);

                if (startIndex < endIndex && startIndex >= 0 && endIndex >= 0)
                {
                    string optionValue = remainingContent.Substring(startIndex, endIndex - startIndex).Trim().Trim(new char[] { ',', ' ' });
                    Jeopartyqa.QuestionsOptions.Add(markers[i].Trim('[', ']')[0], optionValue);
                }
            }

            // Process correct answer
            int correctAnswerIndex = remainingContent.IndexOf("Correct Answer:") + "Correct Answer:".Length;
            string correctAnswerPart = remainingContent.Substring(correctAnswerIndex).Trim();
            char answerKey = correctAnswerPart[1];
            string answerText = correctAnswerPart.Substring(3).Split('.')[0].Trim();

            Jeopartyqa.Answer = answerKey;
            Jeopartyqa.AnswerText = answerText;
        }

    }
}
