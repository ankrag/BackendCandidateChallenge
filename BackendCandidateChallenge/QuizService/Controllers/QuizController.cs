using System.Collections.Generic;
using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using QuizService.Model;
using QuizService.Model.Domain;
using System.Linq;

namespace QuizService.Controllers;

[Route("api/quizzes")]
public class QuizController : Controller
{
    private readonly IDbConnection _connection;

    public QuizController(IDbConnection connection)
    {
        _connection = connection;
    }

    // GET api/quizzes
    [HttpGet]
    public IEnumerable<QuizResponseModel> Get()
    {
        const string sql = "SELECT * FROM Quiz;";
        var quizzes = _connection.Query<Quiz>(sql);
        return quizzes.Select(quiz =>
            new QuizResponseModel
            {
                Id = quiz.Id,
                Title = quiz.Title
            });
    }

    // GET api/quizzes/5
    [HttpGet("{id}")]
    public object Get(int id)
    {        
        var quiz = GetQuizById(id);                
        if (quiz == null)
            return NotFound();        
        var questions = GetQuestionsByQuizId(quiz.Id);        
        var answers = GetAnswersByQuizId(quiz.Id);

        return new QuizResponseModel(quiz, questions, answers);        
    }

    // POST api/quizzes
    [HttpPost]
    public IActionResult Post([FromBody]QuizCreateModel value)
    {
        var sql = $"INSERT INTO Quiz (Title) VALUES('{value.Title}'); SELECT LAST_INSERT_ROWID();";
        //TODO This is vulnerable to SQL injection. Use DynamicParameters or at least use ExecuteScalar with parameters. See GetQuizById for example
        var id = _connection.ExecuteScalar(sql);
        return Created($"/api/quizzes/{id}", null);
    }

    // PUT api/quizzes/5
    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody]QuizUpdateModel value)
    {
        const string sql = "UPDATE Quiz SET Title = @Title WHERE Id = @Id";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        int rowsUpdated = _connection.Execute(sql, new {Id = id, Title = value.Title});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        const string sql = "DELETE FROM Quiz WHERE Id = @Id";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        int rowsDeleted = _connection.Execute(sql, new {Id = id});
        if (rowsDeleted == 0)
            return NotFound();
        return NoContent();
    }

    // POST api/quizzes/5/questions
    [HttpPost]
    [Route("{id}/questions")]
    public IActionResult PostQuestion(int id, [FromBody]QuestionCreateModel value)
    {
        // Check if the quiz exists
        var quiz = GetQuizById(id);
        if (quiz == null)
        {
            return NotFound();
        }

        // Insert the new Question into the database
        const string sql = "INSERT INTO Question (Text, QuizId) VALUES(@Text, @QuizId); SELECT LAST_INSERT_ROWID();";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        var questionId = _connection.ExecuteScalar(sql, new {Text = value.Text, QuizId = id});
        return Created($"/api/quizzes/{id}/questions/{questionId}", null);
    }

    // PUT api/quizzes/5/questions/6
    [HttpPut("{id}/questions/{qid}")]
    public IActionResult PutQuestion(int id, int qid, [FromBody]QuestionUpdateModel value)
    {
        const string sql = "UPDATE Question SET Text = @Text, CorrectAnswerId = @CorrectAnswerId WHERE Id = @QuestionId";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        int rowsUpdated = _connection.Execute(sql, new {QuestionId = qid, Text = value.Text, CorrectAnswerId = value.CorrectAnswerId});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6
    [HttpDelete]
    [Route("{id}/questions/{qid}")]
    public IActionResult DeleteQuestion(int id, int qid)
    {
        const string sql = "DELETE FROM Question WHERE Id = @QuestionId";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        _connection.ExecuteScalar(sql, new {QuestionId = qid});
        return NoContent();
    }

    // POST api/quizzes/5/questions/6/answers
    [HttpPost]
    [Route("{id}/questions/{qid}/answers")]
    public IActionResult PostAnswer(int id, int qid, [FromBody]AnswerCreateModel value)
    {
        const string sql = "INSERT INTO Answer (Text, QuestionId) VALUES(@Text, @QuestionId); SELECT LAST_INSERT_ROWID();";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        var answerId = _connection.ExecuteScalar(sql, new {Text = value.Text, QuestionId = qid});
        return Created($"/api/quizzes/{id}/questions/{qid}/answers/{answerId}", null);
    }

    // PUT api/quizzes/5/questions/6/answers/7
    [HttpPut("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult PutAnswer(int id, int qid, int aid, [FromBody]AnswerUpdateModel value)
    {
        const string sql = "UPDATE Answer SET Text = @Text WHERE Id = @AnswerId";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        int rowsUpdated = _connection.Execute(sql, new {AnswerId = qid, Text = value.Text});
        if (rowsUpdated == 0)
            return NotFound();
        return NoContent();
    }

    // DELETE api/quizzes/5/questions/6/answers/7
    [HttpDelete]
    [Route("{id}/questions/{qid}/answers/{aid}")]
    public IActionResult DeleteAnswer(int id, int qid, int aid)
    {
        const string sql = "DELETE FROM Answer WHERE Id = @AnswerId";
        //TODO I prefer using DynamicParameters to prevent SQL injection. See GetQuizById for example
        _connection.ExecuteScalar(sql, new {AnswerId = aid});
        return NoContent();
    }

    private Quiz GetQuizById(int id)
    {
        const string quizSql = "SELECT * FROM Quiz WHERE Id = @Id;";
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);

        return _connection.QuerySingleOrDefault<Quiz>(quizSql, parameters);
    }

    private IEnumerable<Question> GetQuestionsByQuizId(int quizId)
    {
        const string questionsSql = "SELECT * FROM Question WHERE QuizId = @QuizId;";
        var parameters = new DynamicParameters();
        parameters.Add("@QuizId", quizId);

        return _connection.Query<Question>(questionsSql, parameters);
    }

    private IDictionary<int, IList<Answer>> GetAnswersByQuizId(int quizId)
    {
        const string answersSql = "SELECT a.Id, a.Text, a.QuestionId FROM Answer a INNER JOIN Question q ON a.QuestionId = q.Id WHERE q.QuizId = @QuizId;";
        var parameters = new DynamicParameters();
        parameters.Add("@QuizId", quizId);

        return _connection.Query<Answer>(answersSql, parameters)
            .Aggregate(new Dictionary<int, IList<Answer>>(), (dict, answer) => {
                if (!dict.ContainsKey(answer.QuestionId))
                    dict.Add(answer.QuestionId, new List<Answer>());
                dict[answer.QuestionId].Add(answer);
                return dict;
            });
    }
}