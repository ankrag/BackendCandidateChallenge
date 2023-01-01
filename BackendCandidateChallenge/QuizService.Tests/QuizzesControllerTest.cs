using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using QuizService.Model;
using Xunit;

namespace QuizService.Tests;

public class QuizzesControllerTest
{
    const string QuizApiEndPoint = "/api/quizzes/";

    [Fact]
    public async Task PostNewQuizAddsQuiz()
    {
        var quiz = new QuizCreateModel("Test title");
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(quiz));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"),
                content);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
        }
    }

    [Fact]
    public async Task AQuizExistGetReturnsQuiz()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 1;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var quiz = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
            Assert.Equal(quizId, quiz.Id);
            Assert.Equal("My first quiz", quiz.Title);
        }
    }

    [Fact]
    public async Task AQuizDoesNotExistGetFails()
    {
        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            const long quizId = 999;
            var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}{quizId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
        
    public async Task AQuizDoesNotExists_WhenPostingAQuestion_ReturnsNotFound()
    {
        const string QuizApiEndPoint = "/api/quizzes/999/questions";

        using (var testHost = new TestServer(new WebHostBuilder()
                   .UseStartup<Startup>()))
        {
            var client = testHost.CreateClient();
            //const long quizId = 999;
            var question = new QuestionCreateModel("The answer to everything is what?");
            var content = new StringContent(JsonConvert.SerializeObject(question));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await client.PostAsync(new Uri(testHost.BaseAddress, $"{QuizApiEndPoint}"),content);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetQuizVerifyScoreOfCorrectAnswers()
    {
        // Arrange
        var testHost = new TestServer(new WebHostBuilder()
                       .UseStartup<Startup>());
        var client = testHost.CreateClient();
        int quizId = 1;

        // Act
        var response = await client.GetAsync(new Uri(testHost.BaseAddress, $"api/quizzes/{quizId}"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        var result = JsonConvert.DeserializeObject<QuizResponseModel>(await response.Content.ReadAsStringAsync());
        Assert.Equal(quizId, result.Id);

        // Create a dictionary with the correct answers to the quiz
        var correctAnswers = new Dictionary<int, int>
        {
            { 1, 1 }, // Question 1: Correct answer is 1            
            { 2, 5 }, // Question 2: Correct answer is 5            
        };

        // Iterate through the questions and check if the answers are correct
        int score = 0;        
        foreach (var question in result.Questions)
        {
            int selectedAnswerId = question.CorrectAnswerId;
            int correctAnswerId = correctAnswers[question.Id];

            if (selectedAnswerId == correctAnswerId)
            {
                score++;
            }
        }

        // Assert that the score is 2
        Assert.Equal(2, score);
    }
}