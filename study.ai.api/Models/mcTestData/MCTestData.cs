using Newtonsoft.Json;

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
        public List<Option> Options { get; set; }

        [JsonProperty("correctAnswer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("answerDescription")]
        public string AnswerDescription { get; set; }
    }

    public class Option
    {
        [JsonProperty("option")]
        public string OptionLetter { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
