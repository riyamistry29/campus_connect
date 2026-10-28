using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;

namespace CampusConnect.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "Organizer", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Users
        // Admin
        var adminEmail = "admin@campusconnect.edu";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Dr. Robert Vance",
                Department = "Academic Affairs & Administration",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Organizer
        var organizerEmail = "organizer@campusconnect.edu";
        var organizerUser = await userManager.FindByEmailAsync(organizerEmail);
        if (organizerUser == null)
        {
            organizerUser = new ApplicationUser
            {
                UserName = organizerEmail,
                Email = organizerEmail,
                FullName = "Prof. Sarah Jenkins",
                Department = "Computer Science & Engineering",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(organizerUser, "Organizer@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(organizerUser, "Organizer");
            }
        }

        // Student 1
        var studentEmail = "student@campusconnect.edu";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FullName = "Alex Morgan",
                Department = "Information Technology",
                StudentIdentifier = "IT-2024-045",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(studentUser, "Student@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }

        // Student 2
        var student2Email = "priya@campusconnect.edu";
        var student2User = await userManager.FindByEmailAsync(student2Email);
        if (student2User == null)
        {
            student2User = new ApplicationUser
            {
                UserName = student2Email,
                Email = student2Email,
                FullName = "Priya Sharma",
                Department = "Computer Science",
                StudentIdentifier = "CS-2024-102",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(student2User, "Student@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student2User, "Student");
            }
        }

        // 3. Seed Categories
        if (!context.Categories.Any())
        {
            var categories = new List<EventCategory>
            {
                new() { Name = "Technical", Description = "Hackathons, coding contests, robotic challenges, and tech showcases", IconClass = "bi-laptop" },
                new() { Name = "Cultural", Description = "Music concerts, dance showcases, theater, and arts exhibitions", IconClass = "bi-music-note-beamed" },
                new() { Name = "Sports", Description = "Inter-college tournaments, track & field, and e-sports", IconClass = "bi-trophy" },
                new() { Name = "Workshops", Description = "Hands-on skill building, development, and engineering bootcamps", IconClass = "bi-tools" },
                new() { Name = "Seminars", Description = "Guest lectures, academic symposiums, and keynote speeches", IconClass = "bi-easel" },
                new() { Name = "Festivals", Description = "Annual college fests, cultural nights, and carnivals", IconClass = "bi-stars" },
                new() { Name = "Competitions", Description = "Quizzes, business case challenges, and debating championships", IconClass = "bi-award" },
                new() { Name = "Career & Placement", Description = "Recruitment drives, resume clinics, and corporate networking sessions", IconClass = "bi-briefcase" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 4. Seed Events
        if (!context.Events.Any() && organizerUser != null)
        {
            var techCat = context.Categories.First(c => c.Name == "Technical");
            var culturalCat = context.Categories.First(c => c.Name == "Cultural");
            var sportsCat = context.Categories.First(c => c.Name == "Sports");
            var workshopCat = context.Categories.First(c => c.Name == "Workshops");
            var seminarCat = context.Categories.First(c => c.Name == "Seminars");
            var careerCat = context.Categories.First(c => c.Name == "Career & Placement");

            var now = DateTime.UtcNow;

            var events = new List<CollegeEvent>
            {
                new()
                {
                    Title = "CodeSpark 2026: 36-Hour National Hackathon",
                    Description = "The flagship annual hackathon of CampusConnect! Build next-generation web apps, AI tools, and IoT solutions with industry mentors from top tech firms. Food, swag, and $10,000 in prizes included.",
                    BannerUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(7).AddHours(9),
                    EndDateTime = now.AddDays(8).AddHours(21),
                    Venue = "Advanced Computing Center, Block C - Innovation Lab",
                    Capacity = 150,
                    RegistrationDeadline = now.AddDays(5),
                    Eligibility = "Open to all undergraduate & postgraduate students",
                    Status = EventStatus.Published,
                    CategoryId = techCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-10)
                },
                new()
                {
                    Title = "Campus Symphony: Spring Music & Cultural Gala",
                    Description = "An enchanting evening featuring live acoustic performances, classical ensembles, contemporary rock bands, and inter-department dance battles under the campus amphitheater stars.",
                    BannerUrl = "https://images.unsplash.com/photo-1465847899084-d164df4dedc6?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(12).AddHours(17),
                    EndDateTime = now.AddDays(12).AddHours(22),
                    Venue = "Open Air Amphitheater & Student Activity Hub",
                    Capacity = 500,
                    RegistrationDeadline = now.AddDays(10),
                    Eligibility = "All students, faculty, and registered alumni",
                    Status = EventStatus.Published,
                    CategoryId = culturalCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-5)
                },
                new()
                {
                    Title = "Generative AI & LLM Systems: Hands-On Masterclass",
                    Description = "Learn how to build real-world intelligent agents, fine-tune models, and deploy RAG pipelines using modern frameworks and open APIs. Bring your laptop and your enthusiasm!",
                    BannerUrl = "https://images.unsplash.com/photo-1677442136019-21780ecad995?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(3).AddHours(14),
                    EndDateTime = now.AddDays(3).AddHours(18),
                    Venue = "Seminar Hall 204, Ramanujan Complex",
                    Capacity = 60,
                    RegistrationDeadline = now.AddDays(2),
                    Eligibility = "Basic Python knowledge recommended",
                    Status = EventStatus.Published,
                    CategoryId = workshopCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-4)
                },
                new()
                {
                    Title = "Inter-Department 3v3 Basketball Tournament",
                    Description = "High-octane hoops action! Compete for the prestigious CampusConnect Cup with your department squad. Trophies, medals, and official certificates for winners and runners-up.",
                    BannerUrl = "https://images.unsplash.com/photo-1546519638-68e109498ffc?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(15).AddHours(10),
                    EndDateTime = now.AddDays(16).AddHours(19),
                    Venue = "Indoor Sports Pavilion, Court 1 & 2",
                    Capacity = 80,
                    RegistrationDeadline = now.AddDays(13),
                    Eligibility = "Registered college department teams",
                    Status = EventStatus.Published,
                    CategoryId = sportsCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-3)
                },
                new()
                {
                    Title = "Annual Placement & Career Conclave 2026",
                    Description = "Meet recruiters, HR directors, and alumni leaders from Fortune 500 companies and high-growth startups. Receive 1-on-1 resume reviews and attend fast-track preliminary interviews.",
                    BannerUrl = "https://images.unsplash.com/photo-1515187029135-18ee286d815b?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(20).AddHours(9),
                    EndDateTime = now.AddDays(20).AddHours(17),
                    Venue = "Main Auditorium & Exhibition Atrium",
                    Capacity = 400,
                    RegistrationDeadline = now.AddDays(18),
                    Eligibility = "Final and pre-final year students of all branches",
                    Status = EventStatus.Published,
                    CategoryId = careerCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-2)
                },
                new()
                {
                    Title = "TEDxCampus: Pioneering Human Potential",
                    Description = "Distinguished visionaries, researchers, and social entrepreneurs gather to share thought-provoking ideas worth spreading. A transformative afternoon of talks and networking.",
                    BannerUrl = "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(25).AddHours(13),
                    EndDateTime = now.AddDays(25).AddHours(18),
                    Venue = "Sir C.V. Raman Memorial Auditorium",
                    Capacity = 250,
                    RegistrationDeadline = now.AddDays(22),
                    Eligibility = "Open to all verified college community members",
                    Status = EventStatus.PendingApproval,
                    CategoryId = seminarCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-1)
                },
                new()
                {
                    Title = "RoboWars: Combat Robot Arena Battles",
                    Description = "15kg and 30kg combat robotics showdown! Watch custom-built kinetic spinner bots and flippers battle inside a bulletproof polycarbonate arena.",
                    BannerUrl = "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?auto=format&fit=crop&w=1200&q=80",
                    StartDateTime = now.AddDays(-5).AddHours(10),
                    EndDateTime = now.AddDays(-5).AddHours(17),
                    Venue = "Mechanical Workshop Grounds Arena",
                    Capacity = 120,
                    RegistrationDeadline = now.AddDays(-7),
                    Eligibility = "Robotics club & student teams",
                    Status = EventStatus.Published,
                    CategoryId = techCat.Id,
                    OrganizerId = organizerUser.Id,
                    CreatedAt = now.AddDays(-20)
                }
            };

            await context.Events.AddRangeAsync(events);
            await context.SaveChangesAsync();

            // 5. Seed sample Registrations
            var hackathon = events[0];
            var aiWorkshop = events[2];
            var pastRoboWars = events[6];

            if (studentUser != null)
            {
                var reg1 = new EventRegistration
                {
                    EventId = hackathon.Id,
                    StudentId = studentUser.Id,
                    RegisteredAt = now.AddDays(-2),
                    Status = RegistrationStatus.Confirmed,
                    AttendanceStatus = AttendanceStatus.NotMarked
                };

                var reg2 = new EventRegistration
                {
                    EventId = aiWorkshop.Id,
                    StudentId = studentUser.Id,
                    RegisteredAt = now.AddDays(-1),
                    Status = RegistrationStatus.Confirmed,
                    AttendanceStatus = AttendanceStatus.NotMarked
                };

                var reg3 = new EventRegistration
                {
                    EventId = pastRoboWars.Id,
                    StudentId = studentUser.Id,
                    RegisteredAt = now.AddDays(-15),
                    Status = RegistrationStatus.Confirmed,
                    AttendanceStatus = AttendanceStatus.Present,
                    AttendanceMarkedAt = now.AddDays(-5)
                };

                await context.Registrations.AddRangeAsync(reg1, reg2, reg3);
            }

            if (student2User != null)
            {
                var reg4 = new EventRegistration
                {
                    EventId = hackathon.Id,
                    StudentId = student2User.Id,
                    RegisteredAt = now.AddDays(-3),
                    Status = RegistrationStatus.Confirmed,
                    AttendanceStatus = AttendanceStatus.NotMarked
                };

                var reg5 = new EventRegistration
                {
                    EventId = pastRoboWars.Id,
                    StudentId = student2User.Id,
                    RegisteredAt = now.AddDays(-14),
                    Status = RegistrationStatus.Confirmed,
                    AttendanceStatus = AttendanceStatus.Present,
                    AttendanceMarkedAt = now.AddDays(-5)
                };

                await context.Registrations.AddRangeAsync(reg4, reg5);
            }

            // 6. Seed sample Announcements
            var announcement1 = new EventAnnouncement
            {
                EventId = hackathon.Id,
                Title = "CodeSpark Hackathon Discord Server & Mentor Office Hours",
                Message = "Welcome all registered hackers! Please join our official Discord server at discord.gg/codespark2026 to find team members and see the schedule of workshops before kickoff.",
                CreatedAt = now.AddDays(-1),
                CreatedById = organizerUser.Id
            };

            var announcement2 = new EventAnnouncement
            {
                EventId = hackathon.Id,
                Title = "Hardware Kits & Cloud Credits Distribution",
                Message = "Raspberry Pi 5 kits and AWS cloud credit vouchers will be distributed on Friday at the registration desk from 8:00 AM onwards.",
                CreatedAt = now.AddHours(-6),
                CreatedById = organizerUser.Id
            };

            await context.Announcements.AddRangeAsync(announcement1, announcement2);
            await context.SaveChangesAsync();
        }
    }
}
