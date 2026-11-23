using ClubRegistration.Mvc.Models;
using System.Collections.Generic;
namespace ClubRegistration.Mvc.Services
{
    public class ClubServiceFallback{public List<ClubCard> Empty(){return new List<ClubCard>();}}
    public class EnrollmentServiceFallback{public List<EnrollmentRow> Empty(){return new List<EnrollmentRow>();}}
}
