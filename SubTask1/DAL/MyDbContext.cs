using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace webapp_f26_exam.DAL;

public class MyDbContext : IdentityDbContext<IdentityUser>
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }
}
