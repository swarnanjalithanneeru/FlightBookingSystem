using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PassengerService.Data;

#nullable disable

namespace PassengerService.Migrations
{
    [DbContext(typeof(PassengerDbContext))]
    partial class PassengerDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("PassengerService.Entities.CheckIn", b =>
                {
                    b.Property<int>("CheckInId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CheckInId"));

                    b.Property<int>("BookingId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CheckInDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<string>("CheckInStatus")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasDefaultValue("CheckedIn");

                    b.Property<int>("PassengerId")
                        .HasColumnType("int");

                    b.HasKey("CheckInId");

                    b.HasIndex("BookingId");

                    b.HasIndex("PassengerId")
                        .IsUnique();

                    b.ToTable("CheckIn", (string)null);
                });

            modelBuilder.Entity("PassengerService.Entities.Passenger", b =>
                {
                    b.Property<int>("PassengerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("PassengerId"));

                    b.Property<int>("Age")
                        .HasColumnType("int");

                    b.Property<int>("BookingId")
                        .HasColumnType("int");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("PassengerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SeatNumber")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("nvarchar(5)");

                    b.HasKey("PassengerId");

                    b.HasIndex("BookingId");

                    b.ToTable("Passenger", (string)null);
                });

            modelBuilder.Entity("PassengerService.Entities.CheckIn", b =>
                {
                    b.HasOne("PassengerService.Entities.Passenger", "Passenger")
                        .WithOne("CheckIn")
                        .HasForeignKey("PassengerService.Entities.CheckIn", "PassengerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Passenger");
                });

            modelBuilder.Entity("PassengerService.Entities.Passenger", b =>
                {
                    b.Navigation("CheckIn");
                });
#pragma warning restore 612, 618
        }
    }
}
