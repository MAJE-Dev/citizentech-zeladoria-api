using CitizenTech.Zeladoria.Models;
using Microsoft.EntityFrameworkCore;

namespace CitizenTech.Zeladoria.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Ocorrencia> Ocorrencias { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<StatusOcorrencia> StatusOcorrencias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relacionamentos
            modelBuilder.Entity<Ocorrencia>()
                .HasOne(o => o.Categoria)
                .WithMany(c => c.Ocorrencias)
                .HasForeignKey(o => o.CategoriaId);

            modelBuilder.Entity<Ocorrencia>()
                .HasOne(o => o.StatusOcorrencia)
                .WithMany(s => s.Ocorrencias)
                .HasForeignKey(o => o.StatusId);

            modelBuilder.Entity<Ocorrencia>()
                .HasOne(o => o.Usuario)
                .WithMany(u => u.Ocorrencias)
                .HasForeignKey(o => o.UsuarioId);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Ocorrencia)
                .WithMany(o => o.Comentarios)
                .HasForeignKey(c => c.OcorrenciaId);

            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data
            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nome = "Iluminação Pública", Descricao = "Problemas em postes" },
                new Categoria { Id = 2, Nome = "Buraco na Via", Descricao = "Crateras no asfalto" }
            );

            modelBuilder.Entity<StatusOcorrencia>().HasData(
                new StatusOcorrencia { Id = 1, DescricaoStatus = "Aberta" },
                new StatusOcorrencia { Id = 2, DescricaoStatus = "Em Andamento" }
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario { Id = 1, Nome = "Carlos Silva", Email = "carlos@email.com", Perfil = "cidadao" },
                new Usuario { Id = 2, Nome = "Ana Gestora", Email = "ana@zeladoria.gov", Perfil = "gestor" }
            );

            modelBuilder.Entity<Ocorrencia>().HasData(
                new Ocorrencia
                {
                    Id = 1,
                    Titulo = "Poste apagado",
                    Descricao = "Poste em frente ao número 450 está apagado.",
                    Endereco = "Rua das Flores, 450",
                    CategoriaId = 1,
                    StatusId = 1,
                    UsuarioId = 1
                }
            );

            modelBuilder.Entity<Comentario>().HasData(
                new Comentario
                {
                    Id = 1,
                    Texto = "Equipe acionada para o local.",
                    OcorrenciaId = 1,
                    UsuarioId = 2
                }
            );
        }
    }
}