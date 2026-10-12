using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Default Users
        // Admin
        var adminEmail = "admin@acxiomcrm.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Manager
        var managerEmail = "manager@acxiomcrm.com";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                FullName = "Vikram Sharma (Sales Manager)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        // Sales Executive
        var salesEmail = "sales@acxiomcrm.com";
        var salesUser = await userManager.FindByEmailAsync(salesEmail);
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = salesEmail,
                Email = salesEmail,
                FullName = "Pooja Patel (Sales Exec)",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser, "Sales@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
            }
        }

        // 3. Seed Sample CRM Records if empty
        if (!context.Customers.Any())
        {
            var c1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Rajesh Gupta",
                CompanyName = "Nexus Infotech Ltd",
                Email = "rajesh.gupta@nexusinfo.com",
                Phone = "9876543210",
                Address = "Plot 42, Electronic City Phase 1",
                City = "Bengaluru",
                State = "Karnataka",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                AssignedTo = salesUser?.Id
            };

            var c2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Ananya Roy",
                CompanyName = "Apex Healthcorp",
                Email = "ananya.roy@apexhealth.org",
                Phone = "9812345678",
                Address = "Tower B, Cyber Gateway",
                City = "Hyderabad",
                State = "Telangana",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                AssignedTo = salesUser?.Id
            };

            var c3 = new Customer
            {
                CustomerCode = "CUST-1003",
                CustomerName = "Sunil Verma",
                CompanyName = "Quantum Logistics Solutions",
                Email = "sunil.verma@quantumlog.in",
                Phone = "9765432109",
                Address = "B-12, Sector 62",
                City = "Noida",
                State = "Uttar Pradesh",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                AssignedTo = managerUser?.Id
            };

            var c4 = new Customer
            {
                CustomerCode = "CUST-1004",
                CustomerName = "Meera Nambiar",
                CompanyName = "Horizon Retail Group",
                Email = "meera.n@horizonretail.com",
                Phone = "9988776655",
                Address = "MG Road Commercial Complex",
                City = "Kochi",
                State = "Kerala",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                AssignedTo = salesUser?.Id
            };

            context.Customers.AddRange(c1, c2, c3, c4);
            await context.SaveChangesAsync();

            // Seed Leads
            var l1 = new Lead
            {
                LeadCode = "LEAD-2001",
                LeadName = "Arjun Kapoor",
                CompanyName = "BlueSky FinTech",
                Email = "arjun.k@blueskyfin.com",
                Phone = "9823456789",
                Source = "Website",
                Status = "Qualified",
                ExpectedValue = 450000,
                CreatedDate = DateTime.UtcNow.AddDays(-12),
                AssignedTo = salesUser?.Id
            };

            var l2 = new Lead
            {
                LeadCode = "LEAD-2002",
                LeadName = "Kavita Rao",
                CompanyName = "Zenith Automation",
                Email = "kavita.rao@zenithauto.io",
                Phone = "9945612345",
                Source = "Referral",
                Status = "Contacted",
                ExpectedValue = 280000,
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                AssignedTo = salesUser?.Id
            };

            var l3 = new Lead
            {
                LeadCode = "LEAD-2003",
                LeadName = "Manish Bhatia",
                CompanyName = "Nova Solar Energies",
                Email = "manish.b@novasolar.com",
                Phone = "9123456780",
                Source = "Cold Call",
                Status = "New",
                ExpectedValue = 650000,
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                AssignedTo = managerUser?.Id
            };

            var l4 = new Lead
            {
                LeadCode = "LEAD-2004",
                LeadName = "Rohit Sen",
                CompanyName = "Delta Media Agency",
                Email = "rohit.sen@deltamedia.com",
                Phone = "9654321876",
                Source = "Advertisement",
                Status = "Lost",
                ExpectedValue = 120000,
                CreatedDate = DateTime.UtcNow.AddDays(-18),
                AssignedTo = salesUser?.Id
            };

            context.Leads.AddRange(l1, l2, l3, l4);
            await context.SaveChangesAsync();

            // Seed Opportunities
            var op1 = new Opportunity
            {
                OpportunityName = "Nexus ERP Cloud Migration",
                CustomerId = c1.CustomerId,
                Amount = 750000,
                Stage = "Negotiation",
                Probability = 75,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                AssignedTo = salesUser?.Id,
                Notes = "Final pricing discussion scheduled with CTO."
            };

            var op2 = new Opportunity
            {
                OpportunityName = "Apex Hospital Patient CRM",
                CustomerId = c2.CustomerId,
                Amount = 520000,
                Stage = "Proposal",
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-14),
                AssignedTo = salesUser?.Id,
                Notes = "Detailed technical proposal submitted."
            };

            var op3 = new Opportunity
            {
                OpportunityName = "Quantum Fleet Management Integration",
                CustomerId = c3.CustomerId,
                Amount = 980000,
                Stage = "Qualification",
                Probability = 30,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(40),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-7),
                AssignedTo = managerUser?.Id,
                Notes = "Requirements gathering completed."
            };

            var op4 = new Opportunity
            {
                OpportunityName = "Horizon Omnichannel Retail Suite",
                CustomerId = c4.CustomerId,
                Amount = 350000,
                Stage = "Won",
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                Status = "Won",
                CreatedDate = DateTime.UtcNow.AddDays(-28),
                AssignedTo = salesUser?.Id,
                Notes = "Contract signed, kickoff meeting next Monday."
            };

            context.Opportunities.AddRange(op1, op2, op3, op4);
            await context.SaveChangesAsync();

            // Seed FollowUps
            var f1 = new FollowUp
            {
                CustomerId = c1.CustomerId,
                OpportunityId = op1.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(1),
                FollowUpType = "Meeting",
                Remarks = "Review commercial SLA and license discounts.",
                Status = "Planned",
                AssignedTo = salesUser?.Id
            };

            var f2 = new FollowUp
            {
                CustomerId = c2.CustomerId,
                OpportunityId = op2.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(3),
                FollowUpType = "Call",
                Remarks = "Follow up on compliance security questionnaire.",
                Status = "Planned",
                AssignedTo = salesUser?.Id
            };

            var f3 = new FollowUp
            {
                LeadId = l2.LeadId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = "Email",
                Remarks = "Send brochure and customer success case studies.",
                Status = "Planned",
                AssignedTo = salesUser?.Id
            };

            context.FollowUps.AddRange(f1, f2, f3);
            await context.SaveChangesAsync();

            // Seed Activities
            var a1 = new Activity
            {
                ActivityType = "Meeting",
                Subject = "Initial Product Demo",
                Description = "Presented CRM sales pipeline and analytics capabilities to decision makers.",
                ActivityDate = DateTime.UtcNow.AddDays(-5),
                CustomerId = c1.CustomerId,
                AssignedTo = salesUser?.Id,
                Status = "Completed"
            };

            var a2 = new Activity
            {
                ActivityType = "Call",
                Subject = "Discovery Call",
                Description = "Discussed current bottlenecks with legacy spreadsheets.",
                ActivityDate = DateTime.UtcNow.AddDays(-3),
                LeadId = l1.LeadId,
                AssignedTo = salesUser?.Id,
                Status = "Completed"
            };

            context.Activities.AddRange(a1, a2);
            await context.SaveChangesAsync();

            // Seed Initial Audit Logs
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    UserId = adminUser?.Id,
                    UserName = "admin@acxiomcrm.com",
                    Action = "Security",
                    EntityName = "Auth",
                    RecordId = "SYSTEM",
                    NewValue = "Database initialized and seeded with baseline roles and users.",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = salesUser?.Id,
                    UserName = "sales@acxiomcrm.com",
                    Action = "Create",
                    EntityName = "Customer",
                    RecordId = "CUST-1001",
                    NewValue = "Created customer Nexus Infotech Ltd",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = salesUser?.Id,
                    UserName = "sales@acxiomcrm.com",
                    Action = "Create",
                    EntityName = "Opportunity",
                    RecordId = "Nexus ERP Cloud Migration",
                    NewValue = "Created opportunity Amount: 750000, Stage: Negotiation",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    IpAddress = "127.0.0.1"
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
