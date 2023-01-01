using QuizService.Model.Domain;
using System.Collections.Generic;
using System.Linq;

namespace QuizService.Model;

public class QuizResponseModel
{
    public class AnswerItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    public class QuestionItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public IEnumerable<AnswerItem> Answers { get; set; }
        public int CorrectAnswerId { get; set; }
    }

    public long Id { get; set; }
    public string Title { get; set; }
    public IEnumerable<QuestionItem> Questions { get; set; }
    public IDictionary<string, string> Links { get; set; }

    public QuizResponseModel() { }
    public QuizResponseModel(Quiz quiz, IEnumerable<Question> questions, IDictionary<int, IList<Answer>> answers)
    {
        Id = quiz.Id;
        Title = quiz.Title;
        Questions = questions.Select(question => new QuestionItem
        {
            Id = question.Id,
            Text = question.Text,
            Answers = answers.ContainsKey(question.Id)
                ? answers[question.Id].Select(answer => new AnswerItem
                {
                    Id = answer.Id,
                    Text = answer.Text
                })
                : new AnswerItem[0],
            CorrectAnswerId = question.CorrectAnswerId
        });
        Links = new Dictionary<string, string>
        {
            {"self", $"/api/quizzes/{quiz.Id}"},
            {"questions", $"/api/quizzes/{quiz.Id}/questions"}
        };
    }
}