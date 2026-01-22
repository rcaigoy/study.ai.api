using Newtonsoft.Json;
using System.Collections.Generic;

namespace study.ai.api.Models.mcTestData
{
    public class MCTestData
    {
        [JsonProperty("questions")]
        public List<Question> Questions { get; set; }
    }

    public class Question
    {
        [JsonProperty("questionText")]
        public string QuestionText { get; set; }

        [JsonProperty("options")]
        public List<OptionVM> Options { get; set; }

        [JsonProperty("correctAnswer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("answerDescription")]
        public string AnswerDescription { get; set; }
    }

    public class OptionVM
    {
        [JsonProperty("option")]
        public string Option { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
