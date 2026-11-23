using ClubRegistration.Mvc.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ClubRegistration.Tests
{
    public class MvcRegistrationTests
    {
        [Fact]
        public void Registration_RejectsInvalidEmail()
        {
            var model=new RegisterModel{FirstName="Анна",LastName="Иванова",Email="непочта",Phone="123",Password="winter2026",ConfirmPassword="winter2026"};
            var results=new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(model,new ValidationContext(model),results,true));
        }
        [Fact]
        public void Registration_RejectsMismatchedPasswords()
        {
            var model=new RegisterModel{FirstName="Анна",LastName="Иванова",Email="anna@example.com",Phone="123",Password="winter2026",ConfirmPassword="different"};
            var results=new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(model,new ValidationContext(model),results,true));
        }
        [Fact]
        public void Registration_AllowsValidFields()
        {
            var model=new RegisterModel{FirstName="Анна",LastName="Иванова",Email="anna@example.com",Phone="+79990000000",AccountCategory="student",Password="winter2026",ConfirmPassword="winter2026"};
            var results=new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(model,new ValidationContext(model),results,true));
        }
        [Theory]
        [InlineData("parent")]
        [InlineData("student")]
        [InlineData("college_student")]
        [InlineData("teacher")]
        public void Registration_AllowsSupportedAccountCategory(string category)
        {
            var model=new RegisterModel{FirstName="Анна",LastName="Иванова",Email="anna@example.com",Phone="+79990000000",AccountCategory=category,Password="winter2026",ConfirmPassword="winter2026"};
            var results=new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(model,new ValidationContext(model),results,true));
        }
        [Fact]
        public void Registration_RejectsUnknownAccountCategory()
        {
            var model=new RegisterModel{FirstName="Анна",LastName="Иванова",Email="anna@example.com",Phone="+79990000000",AccountCategory="admin",Password="winter2026",ConfirmPassword="winter2026"};
            var results=new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(model,new ValidationContext(model),results,true));
        }
    }
}
