using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using static WebAppJeopardy.Pages.Game.QuestionModel;

namespace WebAppJeopardy.Pages.Game
{
    public class ResultModel : PageModel
    {
        
        public JeopardyQuestionsAndAnswer Jeoparty { get; set; }


        public void OnGet()
        {
            if (TempData["Jeoparty"] is string serializedAnswer)
            {
                Jeoparty = JsonConvert.DeserializeObject<JeopardyQuestionsAndAnswer>(serializedAnswer);
            }



        }

        public IActionResult OnPostSubmit()
        {
            return RedirectToPage("/Game/Question");

        }
    }
}
