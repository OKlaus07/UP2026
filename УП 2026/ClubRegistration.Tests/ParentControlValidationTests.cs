using ClubRegistration.Mvc.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ClubRegistration.Tests
{
    public class ParentControlValidationTests
    {
        [Fact]
        public void ParentCannotSaveStudentProfileWithoutRequiredName()
        {
            var model = new FamilyStudentEditModel
            {
                StudentId = 12,
                FirstName = " ",
                LastName = "Иванов",
                Phone = "+79990000000"
            };

            var errors = new List<ValidationResult>();

            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        }

        [Fact]
        public void AdminAccountFormRejectsShortPassword()
        {
            var model = new AdminCreateUserModel
            {
                FirstName = "Анна",
                LastName = "Иванова",
                Email = "anna@example.com",
                Password = "123",
                AccountCategory = "student",
                Role = "user"
            };

            var errors = new List<ValidationResult>();

            Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        }
    }
}
