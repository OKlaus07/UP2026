using ClubRegistration.Mvc.Services;
using System;
using Xunit;

namespace ClubRegistration.Tests
{
    public class MvcBusinessRulesTests
    {
        [Fact] public void Category_RequiresName(){Assert.NotNull(BusinessRules.ValidateCategory(" "));}
        [Fact] public void Category_RejectsNameLongerThanDatabaseLimit(){Assert.NotNull(BusinessRules.ValidateCategory(new string('А',101)));}
        [Fact] public void Category_AcceptsValidName(){Assert.Null(BusinessRules.ValidateCategory("Спорт"));}
        [Fact] public void Club_RequiresName(){Assert.NotNull(BusinessRules.ValidateClub("",7,15,0,5));}
        [Fact] public void Club_RequiresAvailablePlaces(){Assert.NotNull(BusinessRules.ValidateClub("Шахматы",7,15,0,0));}
        [Fact] public void Club_RejectsWrongAgeRange(){Assert.NotNull(BusinessRules.ValidateClub("Шахматы",16,8,0,5));}
        [Fact] public void Club_AcceptsCorrectInformation(){Assert.Null(BusinessRules.ValidateClub("Шахматы",7,15,500,8));}
        [Fact] public void Schedule_RejectsInvalidWeekday(){Assert.False(BusinessRules.IsScheduleValid(8,TimeSpan.FromHours(10),TimeSpan.FromHours(11)));}
        [Fact] public void Schedule_RequiresEndAfterStart(){Assert.False(BusinessRules.IsScheduleValid(2,TimeSpan.FromHours(12),TimeSpan.FromHours(11)));}
        [Fact] public void Schedule_DetectsOverlap(){Assert.True(BusinessRules.HasScheduleConflict(2,TimeSpan.FromHours(10.5),TimeSpan.FromHours(11.5),2,TimeSpan.FromHours(11),TimeSpan.FromHours(12)));}
        [Fact] public void Schedule_AllowsAdjacentLessons(){Assert.False(BusinessRules.HasScheduleConflict(2,TimeSpan.FromHours(10),TimeSpan.FromHours(11),2,TimeSpan.FromHours(11),TimeSpan.FromHours(12)));}
        [Fact] public void Enrollment_ApprovalNeedsFreePlace(){Assert.False(BusinessRules.CanApprove("pending",4,4));}
        [Fact] public void Enrollment_OnlyPendingCanBeApproved(){Assert.False(BusinessRules.CanApprove("cancelled",1,4));}
        [Fact] public void Enrollment_ApprovesWhenSeatIsFree(){Assert.True(BusinessRules.CanApprove("pending",3,4));}
        [Fact] public void Password_IsStoredAsSaltedHash(){var salt=PasswordService.NewSalt();var hash=PasswordService.Hash("snowy2026",salt);Assert.NotEqual("snowy2026",hash);Assert.True(PasswordService.Verify("snowy2026",salt,hash));}
        [Fact] public void Password_RejectsWrongValue(){var salt=PasswordService.NewSalt();var hash=PasswordService.Hash("snowy2026",salt);Assert.False(PasswordService.Verify("wrongpass",salt,hash));}
        [Fact] public void Password_UsesUniqueSalt(){var a=PasswordService.NewSalt();var b=PasswordService.NewSalt();Assert.NotEqual(PasswordService.Hash("snowy2026",a),PasswordService.Hash("snowy2026",b));}
    }
}
