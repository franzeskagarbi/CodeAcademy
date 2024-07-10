using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CodeAcademy.Models;

public partial class AcademyContext : DbContext
{
    public AcademyContext()
    {
    }

    public AcademyContext(DbContextOptions<AcademyContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Administrator> Administrators { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseHasStudent> CourseHasStudents { get; set; }

    public virtual DbSet<CourseSection> CourseSections { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-FNPGPDV\\SQLEXPRESS;Database=academy;Trusted_Connection=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrator>(entity =>
        {
            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithOne(p => p.Administrator)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_administrator_user");
        });

        modelBuilder.Entity<Answer>(entity =>
        {
            entity.Property(e => e.AnswerId).ValueGeneratedNever();
            entity.Property(e => e.IsCorrect).IsFixedLength();

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_answers_questions");
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.ToTable("student_answers"); // Specify the table name in the database

            entity.HasKey(e => e.AnswerId).HasName("PK_student_answers"); // Primary key

            // Configure other columns and relationships
            entity.Property(e => e.AnswerId).HasColumnName("answerId");
            entity.Property(e => e.StudentId).HasColumnName("studentId");
            entity.Property(e => e.QuizId).HasColumnName("quizId");
            entity.Property(e => e.QuestionId).HasColumnName("questionId");
            entity.Property(e => e.ChosenAnswerId).HasColumnName("chosen_answer_id");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");

            // Relationships
            entity.HasOne(d => d.ChosenAnswer)
                  .WithMany() // Assuming no navigation property back to StudentAnswer from Answer
                  .HasForeignKey(d => d.ChosenAnswerId)
                  .HasConstraintName("FK_student_answers_q_answers");

            entity.HasOne(d => d.Question)
                  .WithMany(q => q.StudentAnswers) // Assuming Question has a navigation property to StudentAnswer
                  .HasForeignKey(d => d.QuestionId)
                  .HasConstraintName("FK_student_answers_questions");

            entity.HasOne(d => d.Quiz)
                  .WithMany(q => q.StudentAnswers) // Assuming Quiz has a navigation property to StudentAnswer
                  .HasForeignKey(d => d.QuizId)
                  .HasConstraintName("FK_student_answers_quiz");

            entity.HasOne(d => d.Student)
                  .WithMany(s => s.StudentAnswers) // Assuming Student has a navigation property to StudentAnswer
                  .HasForeignKey(d => d.StudentId)
                  .HasConstraintName("FK_student_answers_student");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(e => e.CourseId).ValueGeneratedNever();

            entity.HasOne(d => d.Teacher).WithMany(p => p.Courses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_course_teacher");
        });

        modelBuilder.Entity<CourseHasStudent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.Course).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_course_has_student_course");

            entity.HasOne(d => d.Student).WithMany()
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_course_has_student_student");
        });

        modelBuilder.Entity<CourseSection>(entity =>
        {
            entity.Property(e => e.SectionId).ValueGeneratedNever();

            entity.HasOne(d => d.Course).WithMany(p => p.CourseSections)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_courseSections_course");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.Property(e => e.GradeId).ValueGeneratedNever();

            entity.HasOne(d => d.Quiz).WithMany(p => p.Grades)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_grades_quiz");

            entity.HasOne(d => d.Student).WithMany(p => p.Grades)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_grades_student");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.QuestionId).ValueGeneratedNever();

            entity.HasOne(d => d.Quiz).WithMany(p => p.Questions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_questions_quiz");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.Property(e => e.QuizId).ValueGeneratedNever();

            entity.HasOne(d => d.Section).WithMany(p => p.Quizzes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_quiz_courseSections");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_student_user");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithOne(p => p.Teacher)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_teacher_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.UserId).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
