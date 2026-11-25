<#
  Executor simples de T-SQL para quem nao tem SSMS (Windows PowerShell 5.1 + System.Data.SqlClient).
  Usa autenticacao do Windows (sem senha). Divide o script em lotes nas linhas "GO".
  Exemplos (a partir da raiz do projeto):
    .\sql\sqlserver\Executar-Sql.ps1 -Banco master -Consulta "CREATE DATABASE GS_CSHARP_ZELADORIA"
    .\sql\sqlserver\Executar-Sql.ps1 -Arquivo .\sql\sqlserver\01_Criar_Estrutura.sql
#>
param(
    [string]$Servidor = '(localdb)\MSSQLLocalDB',
    [string]$Banco = 'GS_CSHARP_ZELADORIA',
    [string]$Arquivo,
    [string]$Consulta
)
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture

if ($Arquivo) { $sql = [IO.File]::ReadAllText((Resolve-Path $Arquivo), [Text.Encoding]::UTF8); $origem = Split-Path $Arquivo -Leaf }
elseif ($Consulta) { $sql = $Consulta; $origem = 'consulta' }
else { throw 'Informe -Arquivo ou -Consulta.' }

$cs = "Server=$Servidor;Database=$Banco;Integrated Security=True;Encrypt=False;Application Name=GS_Zeladoria_Executar-Sql"
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.add_InfoMessage({ param($s, $e) Write-Output ("[mensagem SQL] " + $e.Message) })
Write-Output ("-- {0} | servidor={1} | banco={2} | script={3}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'), $Servidor, $Banco, $origem)
$cn.Open()
try {
    $lotes = [regex]::Split($sql, '(?im)^\s*GO\s*$') | Where-Object { $_.Trim() -ne '' }
    foreach ($lote in $lotes) {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $lote
        $cmd.CommandTimeout = 120
        $da = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
        $ds = New-Object System.Data.DataSet
        [void]$da.Fill($ds)
        foreach ($t in $ds.Tables) {
            if ($t.Rows.Count -eq 0) { Write-Output ("(0 linhas) colunas: " + (($t.Columns | ForEach-Object ColumnName) -join ', ')); continue }
            $t | Format-Table -AutoSize -Wrap | Out-String -Width 400 | Write-Output
        }
    }
    Write-Output "-- OK: $origem executado sem erro."
}
catch {
    Write-Output ("-- ERRO SQL: " + $_.Exception.InnerException.Message + $_.Exception.Message)
    exit 1
}
finally { $cn.Close() }
