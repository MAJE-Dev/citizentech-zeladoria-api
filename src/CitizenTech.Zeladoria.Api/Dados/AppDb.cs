using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CitizenTech.Zeladoria.Api.Dados;

/// <summary>
/// Mapeamento EF Core -> SQL Server, idêntico ao sql/sqlserver/01_Criar_Estrutura.sql.
/// A API não cria tabelas (sem EnsureCreated/Migrate): o DDL é a fonte da estrutura.
/// </summary>
public sealed class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Regiao> Regioes => Set<Regiao>();
    public DbSet<CategoriaOcorrencia> Categorias => Set<CategoriaOcorrencia>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<ImagemOcorrencia> Imagens => Set<ImagemOcorrencia>();
    public DbSet<AtendimentoOcorrencia> Atendimentos => Set<AtendimentoOcorrencia>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Regiao>(b =>
        {
            b.ToTable("REGIAO"); b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(100);
            b.Property(x => x.Bairro).HasMaxLength(80);
            b.Property(x => x.Cidade).HasMaxLength(80);
        });

        m.Entity<CategoriaOcorrencia>(b =>
        {
            b.ToTable("CATEGORIA_OCORRENCIA"); b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(60);
            b.HasIndex(x => x.Nome).IsUnique();
        });

        m.Entity<Ocorrencia>(b =>
        {
            b.ToTable("OCORRENCIA"); b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).HasMaxLength(100);
            b.Property(x => x.Descricao).HasMaxLength(500);
            b.Property(x => x.Endereco).HasMaxLength(150);
            b.Property(x => x.Unidade).HasMaxLength(10);
            b.Property(x => x.Situacao).HasMaxLength(20);
            b.Property(x => x.QuantidadeEstimada).HasPrecision(12, 3);
            b.Property(x => x.QuantidadePendente).HasPrecision(12, 3);
            b.HasOne<Regiao>().WithMany().HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<CategoriaOcorrencia>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<ImagemOcorrencia>(b =>
        {
            b.ToTable("IMAGEM_OCORRENCIA"); b.HasKey(x => x.Id);
            b.Property(x => x.Titulo).HasMaxLength(100);
            b.Property(x => x.NomeArquivo).HasMaxLength(200);
            b.Property(x => x.Mime).HasMaxLength(30);
            b.Property(x => x.HashSha256).HasMaxLength(64).IsFixedLength().IsUnicode(false); // char(64)
            b.Property(x => x.Dados).HasColumnType("varbinary(max)");                          // bytes reais
            b.Property(x => x.EquipeRm).HasMaxLength(20);
            b.Property(x => x.Executor).HasMaxLength(80);
            b.HasOne<Ocorrencia>().WithMany().HasForeignKey(x => x.OcorrenciaId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<AtendimentoOcorrencia>(b =>
        {
            b.ToTable("ATENDIMENTO_OCORRENCIA"); b.HasKey(x => x.Id);
            b.Property(x => x.Quantidade).HasPrecision(12, 3);
            b.Property(x => x.EquipeCampoCodigo).HasMaxLength(40);
            b.Property(x => x.EquipeRm).HasMaxLength(20);
            b.Property(x => x.Executor).HasMaxLength(80);
            b.HasOne<Ocorrencia>().WithMany().HasForeignKey(x => x.OcorrenciaId).OnDelete(DeleteBehavior.Restrict);
        });

        // Nomes físicos em maiúsculas, iguais ao DDL; datas como datetime2(6) lidas como UTC.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        foreach (var entidade in m.Model.GetEntityTypes())
            foreach (var propriedade in entidade.GetProperties())
            {
                propriedade.SetColumnName(propriedade.Name.ToUpperInvariant());
                if (propriedade.ClrType == typeof(DateTime))
                {
                    propriedade.SetColumnType("datetime2(6)");
                    propriedade.SetValueConverter(utc);
                }
            }
    }
}
