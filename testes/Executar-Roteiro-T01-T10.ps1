<#
  Roteiro REAL dos testes T01-T10 (exceto T09, que é documental) contra a API + SQL Server.
  Inicia/para a API de verdade (reinício do T02 e queda do T10), executa cada chamada HTTP (curl.exe e o
  cliente C#), consulta o banco ANTES e DEPOIS e grava cada evidência em evidencias\E0x_*.txt.

  Pré-requisitos: banco criado com sql\sqlserver\00..02 (linha de base: ocorrência 1 = 20 pendentes, 0 atendimentos),
  user-secrets configurado (README), dotnet SDK 10, curl.exe e Microsoft Edge (captura da imagem exibida).
  Uso (na raiz do projeto, Windows PowerShell):
    .\testes\Executar-Roteiro-T01-T10.ps1 -Executor 'VitorCardoso_RM556404'
#>
param(
    [string]$Executor = 'VitorCardoso_RM556404',
    [int]$Porta = 5080,
    [string]$Servidor = '(localdb)\MSSQLLocalDB',
    [string]$Banco = 'GS_CSHARP_ZELADORIA'
)
$ErrorActionPreference = 'Continue' # stderr de curl/dotnet (ex.: API parada no T10) é resultado do teste, não erro do roteiro
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8
$Raiz = Split-Path $PSScriptRoot -Parent
Set-Location $Raiz
$Ev = Join-Path $Raiz 'evidencias'
$Saida = Join-Path $Raiz 'testes\saida'
New-Item -ItemType Directory -Force $Saida | Out-Null
$Base = "http://localhost:$Porta"
$ApiProj = Join-Path $Raiz 'src\CitizenTech.Zeladoria.Api'
$ApiDll = Join-Path $ApiProj 'bin\Debug\net10.0\CitizenTech.Zeladoria.Api.dll'
$ClienteDll = Join-Path $Raiz 'src\CitizenTech.Zeladoria.Cliente\bin\Debug\net10.0\CitizenTech.Zeladoria.Cliente.dll'
$Sql = Join-Path $Raiz 'sql\sqlserver\Executar-Sql.ps1'
$Amostra = Join-Path $Raiz 'amostras\buraco_poste_apagado_mooca.jpg'
$script:Arquivo = $null
$script:Resumo = New-Object System.Collections.ArrayList
$env:ZELADORIA_API = $Base

# ---------------- utilitários ----------------
function Novo-Arquivo($nome, $titulo) {
    $script:Arquivo = Join-Path $Ev $nome
    Set-Content -Path $script:Arquivo -Value '' -Encoding UTF8
    L ("=" * 100)
    L $titulo
    L ("Data/hora: {0} | Executor: {1} | API: {2} | Banco: {3} em {4} | Máquina: {5}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'), $Executor, $Base, $Banco, $Servidor, $env:COMPUTERNAME)
    L ("=" * 100)
}
function L($texto) { Write-Host $texto; Add-Content -Path $script:Arquivo -Value $texto -Encoding UTF8 }
function Secao($t) { L ''; L ("--- " + $t + " " + ("-" * [Math]::Max(3, 90 - $t.Length))) }

function SqlQ($consulta) {
    $r = & $Sql -Servidor $Servidor -Banco $Banco -Consulta $consulta
    $r | Where-Object { $_ -notmatch '^-- OK' } | ForEach-Object { L $_ }
}
function SqlValor($consulta) {
    $cn = New-Object System.Data.SqlClient.SqlConnection "Server=$Servidor;Database=$Banco;Integrated Security=True"
    $cn.Open(); try { $c = $cn.CreateCommand(); $c.CommandText = $consulta; return $c.ExecuteScalar() } finally { $cn.Close() }
}
function EstadoOcorrencia1 {
    SqlQ "SELECT o.ID, o.QUANTIDADEESTIMADA, o.QUANTIDADEPENDENTE, o.SITUACAO, (SELECT COUNT(*) FROM ATENDIMENTO_OCORRENCIA a WHERE a.OCORRENCIAID=o.ID) AS ATENDIMENTOS, (SELECT COALESCE(SUM(QUANTIDADE),0) FROM ATENDIMENTO_OCORRENCIA a WHERE a.OCORRENCIAID=o.ID) AS QTD_ATENDIDA, (SELECT COUNT(*) FROM ATENDIMENTO_OCORRENCIA) AS ATENDIMENTOS_TOTAL FROM OCORRENCIA o WHERE o.ID=1"
}

# curl com status separado; corpo completo no log
function Http([string]$descricao, [string[]]$argumentos, [int[]]$esperado) {
    L ''
    L (">>> " + $descricao)
    L ("    curl.exe " + ($argumentos -join ' '))
    $out = & curl.exe -s -S -i --max-time 30 -w "`nHTTP_STATUS=%{http_code}" @argumentos 2>&1
    $status = 0
    foreach ($linha in $out) {
        if ("$linha" -match '^HTTP_STATUS=(\d+)') { $status = [int]$Matches[1] }
        elseif ("$linha" -notmatch '^(Date|Server|Transfer-Encoding):') { L ("    " + $linha) }
    }
    Registrar $descricao $status $esperado
    return $status
}
# JSON vai por arquivo (--data-binary @arquivo): o Windows PowerShell 5.1 remove aspas de argumentos nativos.
function HttpJson([string]$descricao, [string]$url, [string]$json, [int[]]$esperado) {
    $arq = Join-Path $Saida ("corpo_{0}.json" -f ([Guid]::NewGuid().ToString('N').Substring(0, 8)))
    [IO.File]::WriteAllText($arq, $json, (New-Object Text.UTF8Encoding $false))
    L ''
    L ("    corpo JSON enviado: " + $json)
    return (Http $descricao @('-X', 'POST', $url, '-H', 'Content-Type: application/json', '--data-binary', "@$arq") $esperado)
}
function Cliente([string]$descricao, [string[]]$argumentos, [int]$exitEsperado) {
    L ''
    L (">>> " + $descricao)
    L ("    dotnet CitizenTech.Zeladoria.Cliente.dll " + ($argumentos -join ' '))
    $out = & dotnet $ClienteDll @argumentos 2>&1
    $code = $LASTEXITCODE
    $out | ForEach-Object { L ("    " + $_) }
    L ("    (exit code do cliente: {0})" -f $code)
    Registrar $descricao $code @($exitEsperado) 'exit'
    return ($out -join "`n")
}
function Registrar($descricao, $obtido, $esperado, $tipo = 'HTTP') {
    $ok = $esperado -contains $obtido
    $sit = if ($ok) { 'CONFERE' } else { 'DIVERGE' }
    L ("    => Esperado {0} {1} | Observado {0} {2} | {3}" -f $tipo, ($esperado -join '/'), $obtido, $sit)
    [void]$script:Resumo.Add([pscustomobject]@{ Arquivo = (Split-Path $script:Arquivo -Leaf); Verificacao = $descricao; Esperado = ($esperado -join '/'); Observado = $obtido; Situacao = $sit })
}
function Verificar($descricao, [bool]$condicao) {
    $sit = if ($condicao) { 'CONFERE' } else { 'DIVERGE' }
    L ("    => Verificação: {0} | {1}" -f $descricao, $sit)
    [void]$script:Resumo.Add([pscustomobject]@{ Arquivo = (Split-Path $script:Arquivo -Leaf); Verificacao = $descricao; Esperado = 'verdadeiro'; Observado = $condicao; Situacao = $sit })
}

$script:Api = $null; $script:Sessao = 0
function Iniciar-Api([string]$conexaoOverride = $null, [int]$porta = $Porta) {
    $script:Sessao++
    $log = Join-Path $Saida ("api_sessao{0}.log" -f $script:Sessao)
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    if ($conexaoOverride) { $env:ConnectionStrings__Banco = $conexaoOverride } else { Remove-Item Env:\ConnectionStrings__Banco -ErrorAction SilentlyContinue }
    $p = Start-Process -FilePath 'dotnet' -ArgumentList "`"$ApiDll`" --urls http://localhost:$porta" -WorkingDirectory $ApiProj `
        -RedirectStandardOutput $log -RedirectStandardError ($log + '.err') -PassThru -WindowStyle Hidden
    Remove-Item Env:\ConnectionStrings__Banco -ErrorAction SilentlyContinue
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 500
        $code = & curl.exe -s -o NUL -w "%{http_code}" "http://localhost:$porta/api/equipe" 2>$null
        if ($code -eq '200') { break }
    }
    L ("    [API] sessão {0} iniciada: PID {1}, http://localhost:{2}, ambiente Development, log testes\saida\{3}" -f $script:Sessao, $p.Id, $porta, (Split-Path $log -Leaf))
    $script:Api = $p
    return $p
}
function Parar-Api {
    if ($script:Api -and -not $script:Api.HasExited) {
        $id = $script:Api.Id
        Stop-Process -Id $id -Force
        $script:Api.WaitForExit(10000) | Out-Null
        L ("    [API] processo PID {0} encerrado em {1} (API parada)" -f $id, (Get-Date -Format 'HH:mm:ss'))
    }
}
function Trecho-Log($sessao, $inicio, $fim) {
    $log = Join-Path $Saida ("api_sessao{0}.log" -f $sessao)
    $linhas = Get-Content $log -Encoding UTF8
    $a = ($linhas | Select-String -SimpleMatch $inicio | Select-Object -Last 1).LineNumber
    $b = ($linhas | Select-String -SimpleMatch $fim | Where-Object { $_.LineNumber -ge $a } | Select-Object -First 1).LineNumber
    # recua até o "Beginning transaction" do EF Core que antecede o marcador [TX] BEGIN
    $a0 = ($linhas | Select-String -SimpleMatch 'Beginning transaction' | Where-Object { $_.LineNumber -le $a } | Select-Object -Last 1).LineNumber
    if (-not $a0) { $a0 = $a }
    if ($a -and $b) { $linhas[[Math]::Max(0, $a0 - 2)..$b] | ForEach-Object { L ("    | " + $_) } }
    else { L '    (trecho de log não localizado)' }
}

# ---------------- preparação ----------------
$vazio = Join-Path $Saida 'vazio.png'; [IO.File]::WriteAllBytes($vazio, [byte[]]@())
$disfarcado = Join-Path $Saida 'texto_disfarcado.png'; [IO.File]::WriteAllText($disfarcado, 'isto e texto puro, nao uma imagem PNG')
$gif = Join-Path $Saida 'formato_nao_aceito.gif'; [IO.File]::WriteAllBytes($gif, [byte[]](@(0x47,0x49,0x46,0x38,0x39,0x61,1,0,1,0,0x80,0,0) + (,0 * 30)))
$grande = Join-Path $Saida 'grande_2_5MiB.jpg'
$bytes = New-Object byte[] (2621440); (New-Object Random 7).NextBytes($bytes); $bytes[0]=0xFF; $bytes[1]=0xD8; $bytes[2]=0xFF; [IO.File]::WriteAllBytes($grande, $bytes)
[IO.File]::WriteAllText((Join-Path $Saida 'corpo_t10.json'), ('{"quantidade":1,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}'), (New-Object Text.UTF8Encoding $false))
$HashOriginal = (Get-FileHash $Amostra -Algorithm SHA256).Hash
$TamOriginal = (Get-Item $Amostra).Length

# ================= T01 =================
Novo-Arquivo 'E04_01_T01_Consultas_e_Upload.txt' 'T01 - Consultar equipe/ocorrências e enviar imagem válida vinculada ao domínio (ODS 11) | E01, E04'
L 'Esperado: HTTP 200 nas consultas; POST 201 com ID real; SQL confirma bytes e metadados.'
Secao 'API iniciada'
Iniciar-Api | Out-Null
Secao 'Consultas do domínio (GET, JSON)'
Http 'GET /api/equipe (identificação acadêmica)' @("$Base/api/equipe") @(200) | Out-Null
Http 'GET /api/regioes' @("$Base/api/regioes") @(200) | Out-Null
Http 'GET /api/categorias' @("$Base/api/categorias") @(200) | Out-Null
Http 'GET /api/ocorrencias?regiaoId=1 (filtro LINQ)' @("$Base/api/ocorrencias?regiaoId=1") @(200) | Out-Null
Http 'GET /api/ocorrencias/1' @("$Base/api/ocorrencias/1") @(200) | Out-Null
Secao 'Cidadão registra nova ocorrência (POST JSON)'
HttpJson 'POST /api/ocorrencias (JSON)' "$Base/api/ocorrencias" ('{"regiaoId":1,"categoriaId":2,"titulo":"Poste piscando na Rua Exemplo","descricao":"Registro de teste T01 (dado fictício).","endereco":"Rua Exemplo, 50 (Mooca)","unidade":"un","quantidadeEstimada":1,"executor":"' + $Executor + '"}') @(201) | Out-Null
Secao 'Banco ANTES do upload'
SqlQ 'SELECT COUNT(*) AS IMAGENS_ANTES FROM IMAGEM_OCORRENCIA'
Secao 'Upload pelo cliente C# (multipart, StreamContent) para a ocorrência 1'
$r = Cliente 'Cliente: enviar-imagem 1 (JPEG da amostra)' @('enviar-imagem', '1', $Amostra, 'Buraco com agua e postes apagados - Mooca', $Executor) 0
$ImagemId = [int]([regex]::Match($r, '"Id":\s*(\d+)', 'IgnoreCase').Groups[1].Value)
L ("    ID real devolvido pela API: {0}" -f $ImagemId)
Http "GET /api/imagens/$ImagemId (metadados JSON, sem bytes)" @("$Base/api/imagens/$ImagemId") @(200) | Out-Null
Http 'GET /api/ocorrencias/1/imagens' @("$Base/api/ocorrencias/1/imagens") @(200) | Out-Null

Novo-Arquivo 'E04_02_T01_SQL_Bytes_no_Banco.txt' 'T01/E04 - Consulta SQL comprova bytes reais, ID e metadados da imagem gravada'
L ("Arquivo original: amostras\{0} | {1} bytes | SHA-256 {2}" -f (Split-Path $Amostra -Leaf), $TamOriginal, $HashOriginal)
SqlQ "SELECT i.ID, i.OCORRENCIAID, i.TITULO, i.NOMEARQUIVO, i.MIME, i.TAMANHOBYTES, DATALENGTH(i.DADOS) AS BYTES_NO_BANCO, CONVERT(char(64), HASHBYTES('SHA2_256', i.DADOS), 2) AS SHA256_CALCULADO_PELO_SQL, CONVERT(varchar(20), SUBSTRING(i.DADOS,1,4), 1) AS INICIO_JPEG, i.EQUIPERM, i.EXECUTOR, i.ENVIADAUTC FROM IMAGEM_OCORRENCIA i WHERE i.ID = $ImagemId"
$hashSql = SqlValor "SELECT CONVERT(char(64), HASHBYTES('SHA2_256', DADOS), 2) FROM IMAGEM_OCORRENCIA WHERE ID = $ImagemId"
$lenSql = SqlValor "SELECT DATALENGTH(DADOS) FROM IMAGEM_OCORRENCIA WHERE ID = $ImagemId"
Verificar "DATALENGTH(DADOS) = $lenSql = tamanho do arquivo original ($TamOriginal)" ($lenSql -eq $TamOriginal)
Verificar "SHA-256 calculado pelo SQL Server = SHA-256 do arquivo original" ($hashSql -eq $HashOriginal)

# ================= T02 =================
Novo-Arquivo 'E05_01_T02_Reinicio_e_Recuperacao.txt' 'T02 - Anotar ID, parar/reiniciar API e cliente, recuperar por ID sem selecionar o original | E05'
L ("Esperado: GET 200, metadados e imagem exibida vindos do banco; bytes/hash iguais. ID anotado no T01: {0}" -f $ImagemId)
Secao 'Parar a API'
$pidAntigo = $script:Api.Id
Parar-Api
Http 'GET com a API parada (confirma que nada responde)' @("$Base/api/imagens/$ImagemId") @(0) | Out-Null
Secao 'Reiniciar a API (novo processo)'
Iniciar-Api | Out-Null
Verificar ("PID mudou após reinício ({0} -> {1})" -f $pidAntigo, $script:Api.Id) ($pidAntigo -ne $script:Api.Id)
Secao 'Recuperar pelo MESMO ID, sem upload e sem abrir o arquivo original'
Http "GET /api/imagens/$ImagemId (metadados após reinício)" @("$Base/api/imagens/$ImagemId") @(200) | Out-Null
$recuperada = Join-Path $Saida ("recuperada_imagem_{0}.jpg" -f $ImagemId)
Cliente "Cliente (novo processo): baixar-imagem $ImagemId via GET /conteudo (stream -> arquivo)" @('baixar-imagem', "$ImagemId", $recuperada) 0 | Out-Null
$hashRec = (Get-FileHash $recuperada -Algorithm SHA256).Hash
L ("    SHA-256 original   : {0} ({1} bytes)" -f $HashOriginal, $TamOriginal)
L ("    SHA-256 recuperado : {0} ({1} bytes)" -f $hashRec, (Get-Item $recuperada).Length)
Verificar 'Iguais (bytes recuperados da API após reinício = original)' ($hashRec -eq $HashOriginal)
Secao 'Exibição da imagem servida pela API (Microsoft Edge, captura em evidencias\E05_02_T02_Imagem_Exibida_Apos_Reinicio.png)'
$html = Join-Path $Saida 'visualizar_T02.html'
$agora = Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'
[IO.File]::WriteAllText($html, @"
<!doctype html><html lang="pt-br"><meta charset="utf-8"><body style="font-family:Segoe UI,Arial;margin:16px;background:#f4f4f4">
<h2 style="margin:0 0 6px">T02 / E05 - Imagem recuperada do SQL Server pela API após reinício</h2>
<div style="font-size:14px;line-height:1.5">GET $Base/api/imagens/$ImagemId/conteudo &nbsp;|&nbsp; imagem ID <b>$ImagemId</b> (ocorrência 1) &nbsp;|&nbsp; capturado em $agora<br>
API reiniciada: PID $pidAntigo &rarr; PID $($script:Api.Id) &nbsp;|&nbsp; SHA-256 recuperado = original: <b>$($hashRec -eq $HashOriginal)</b> &nbsp;|&nbsp; executor $Executor &nbsp;|&nbsp; equipe Zeladoria API Urbana (3ESPH, líder KaioSouza_RM556095)<br>
<i>A imagem abaixo é carregada pelo navegador diretamente da URL da API (não é o arquivo original nem prévia local).</i></div>
<img src="$Base/api/imagens/$ImagemId/conteudo" style="margin-top:10px;max-width:960px;border:3px solid #333"></body></html>
"@, (New-Object Text.UTF8Encoding $false))
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
$png = Join-Path $Ev 'E05_02_T02_Imagem_Exibida_Apos_Reinicio.png'
$perfil = Join-Path $env:TEMP 'gs_zeladoria_edge'
if (Test-Path $png) { Remove-Item $png }
Start-Process -FilePath $edge -Wait -ArgumentList @('--headless=new', '--disable-gpu', '--hide-scrollbars', "--user-data-dir=`"$perfil`"",
    '--window-size=1000,820', '--virtual-time-budget=8000', "--screenshot=`"$png`"", ("file:///" + ($html -replace '\\', '/')))
for ($i = 0; $i -lt 40 -and -not (Test-Path $png); $i++) { Start-Sleep -Milliseconds 500 }
Verificar 'Captura PNG da exibição gerada' (Test-Path $png)
Secao 'SQL: mesma linha por ID após reinício'
SqlQ "SELECT ID, OCORRENCIAID, NOMEARQUIVO, MIME, TAMANHOBYTES, DATALENGTH(DADOS) AS BYTES_NO_BANCO, HASHSHA256, ENVIADAUTC FROM IMAGEM_OCORRENCIA WHERE ID = $ImagemId"

# ================= T03 =================
Novo-Arquivo 'E08_01_T03_Arquivo_Ausente_Vazio_Titulo.txt' 'T03 - POST sem arquivo, com zero bytes e com título vazio (demais campos válidos) | E08'
L 'Esperado: 4xx com mensagem; nenhum registro novo.'
$antes = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'; L "    Imagens no banco ANTES: $antes"
Http 'POST sem o campo Arquivo' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '--form-string', 'Titulo=Sem arquivo', '--form-string', "Executor=$Executor") @(400) | Out-Null
Http 'POST com arquivo de 0 bytes' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '-F', "Arquivo=@$vazio;type=image/png", '--form-string', 'Titulo=Arquivo vazio', '--form-string', "Executor=$Executor") @(400) | Out-Null
Http 'POST com título vazio (arquivo válido)' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '-F', "Arquivo=@$Amostra;type=image/jpeg", '--form-string', 'Titulo=', '--form-string', "Executor=$Executor") @(400) | Out-Null
$depois = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'; L "    Imagens no banco DEPOIS: $depois"
Verificar 'Nenhum registro novo em IMAGEM_OCORRENCIA' ($antes -eq $depois)

# ================= T04 =================
Novo-Arquivo 'E08_02_T04_Formato_Disfarce_Tamanho.txt' 'T04 - Formato não aceito, .png disfarçado com texto e arquivo entre 2 e 3 MiB | E08'
L 'Esperado: 4xx (415 para formato/assinatura, 413 para excesso); sem novo registro.'
$antes = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'; L "    Imagens no banco ANTES: $antes"
Http 'POST .gif (formato não aceito)' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '-F', "Arquivo=@$gif;type=image/gif", '--form-string', 'Titulo=GIF', '--form-string', "Executor=$Executor") @(415) | Out-Null
Http 'POST .png com bytes de texto (extensão e MIME mentem)' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '-F', "Arquivo=@$disfarcado;type=image/png", '--form-string', 'Titulo=PNG falso', '--form-string', "Executor=$Executor") @(415) | Out-Null
L ("    Arquivo grande: {0} bytes (2,5 MiB)" -f (Get-Item $grande).Length)
Http 'POST .jpg com 2,5 MiB (acima de 2 MiB, abaixo de 3 MiB)' @('-X', 'POST', "$Base/api/ocorrencias/1/imagens", '-F', "Arquivo=@$grande;type=image/jpeg", '--form-string', 'Titulo=Grande', '--form-string', "Executor=$Executor") @(413) | Out-Null
$depois = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'; L "    Imagens no banco DEPOIS: $depois"
Verificar 'Nenhum registro novo em IMAGEM_OCORRENCIA' ($antes -eq $depois)

# ================= T05 =================
Novo-Arquivo 'E08_03_T05_ID_Inexistente_e_Vinculo.txt' 'T05 - Imagem com ID confirmado como ausente e upload para vínculo inexistente | E08'
L 'Esperado: 404 coerente; sem gravação; cliente de teste continua utilizável.'
SqlQ 'SELECT COUNT(*) AS IMAGEM_999999_EXISTE FROM IMAGEM_OCORRENCIA WHERE ID = 999999; SELECT COUNT(*) AS OCORRENCIA_999999_EXISTE FROM OCORRENCIA WHERE ID = 999999'
$antes = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'
Http 'GET /api/imagens/999999 (metadados)' @("$Base/api/imagens/999999") @(404) | Out-Null
Http 'GET /api/imagens/999999/conteudo' @("$Base/api/imagens/999999/conteudo") @(404) | Out-Null
Http 'POST imagem válida para ocorrência 999999 (vínculo inexistente)' @('-X', 'POST', "$Base/api/ocorrencias/999999/imagens", '-F', "Arquivo=@$Amostra;type=image/jpeg", '--form-string', 'Titulo=Vinculo inexistente', '--form-string', "Executor=$Executor") @(404) | Out-Null
Cliente 'Cliente: metadados 999999 (trata 404 sem travar)' @('metadados', '999999') 1 | Out-Null
Cliente 'Cliente segue utilizável: equipe' @('equipe') 0 | Out-Null
$depois = SqlValor 'SELECT COUNT(*) FROM IMAGEM_OCORRENCIA'
Verificar "Nenhum registro novo em IMAGEM_OCORRENCIA ($antes -> $depois)" ($antes -eq $depois)

# ================= T06 =================
Novo-Arquivo 'E08_04_T06_Transacao_Rejeicoes.txt' 'T06 - POST transacional com quantidade zero/negativa, saldo insuficiente e executor não cadastrado | E08'
L 'Esperado: 400/409 documentados; sem redução do pendente e sem novo atendimento. (Rejeições ANTES de escrever comprovam C6, não rollback.)'
Secao 'Banco ANTES'
EstadoOcorrencia1
$url = "$Base/api/ocorrencias/1/atendimentos"
HttpJson 'quantidade 0' $url ('{"quantidade":0,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}') @(400) | Out-Null
HttpJson 'quantidade negativa (-5)' $url ('{"quantidade":-5,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}') @(400) | Out-Null
HttpJson 'quantidade com 4 casas (1.2345)' $url ('{"quantidade":1.2345,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}') @(400) | Out-Null
HttpJson 'saldo insuficiente (999 > pendente)' $url ('{"quantidade":999,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}') @(409) | Out-Null
HttpJson 'executor não cadastrado (Intruso_RM000000)' $url '{"quantidade":1,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"Intruso_RM000000"}' @(400) | Out-Null
HttpJson 'ocorrência inexistente (999999)' "$Base/api/ocorrencias/999999/atendimentos" ('{"quantidade":1,"equipeCampoCodigo":"EQ_ZEL_MOOCA_01","executor":"' + $Executor + '"}') @(404) | Out-Null
Secao 'Banco DEPOIS'
EstadoOcorrencia1
Verificar 'Pendente da ocorrência 1 continua 20.000' ((SqlValor 'SELECT QUANTIDADEPENDENTE FROM OCORRENCIA WHERE ID=1') -eq 20)
Verificar 'Nenhum atendimento gravado' ((SqlValor 'SELECT COUNT(*) FROM ATENDIMENTO_OCORRENCIA') -eq 0)

# ================= T07 =================
Novo-Arquivo 'E06_01_T07_COMMIT.txt' 'T07 - Operação válida da API com duas alterações (UPDATE OCORRENCIA + INSERT ATENDIMENTO) -> COMMIT | E06'
L 'Esperado: COMMIT; ambas persistem. Ocorrência 1: 20.000 m2 e 0 atendimentos -> 17.000 m2 e 1 atendimento.'
Secao 'ANTES (linha de base registrada)'
EstadoOcorrencia1
Secao 'CHAMADA (cliente C#, JSON)'
Cliente 'Cliente: atender 1 3 EQ_ZEL_MOOCA_01 (sem falha)' @('atender', '1', '3', 'EQ_ZEL_MOOCA_01', $Executor) 0 | Out-Null
Secao 'DEPOIS'
EstadoOcorrencia1
SqlQ 'SELECT ID, OCORRENCIAID, QUANTIDADE, EQUIPECAMPOCODIGO, EQUIPERM, EXECUTOR, CRIADOUTC FROM ATENDIMENTO_OCORRENCIA ORDER BY ID'
Verificar 'Pendente = 17.000 após COMMIT' ((SqlValor 'SELECT QUANTIDADEPENDENTE FROM OCORRENCIA WHERE ID=1') -eq 17)
Verificar '1 atendimento persistido' ((SqlValor 'SELECT COUNT(*) FROM ATENDIMENTO_OCORRENCIA WHERE OCORRENCIAID=1') -eq 1)

# ================= T08 =================
Novo-Arquivo 'E07_01_T08_ROLLBACK.txt' 'T08 - Falha controlada na API DEPOIS do UPDATE e do INSERT, antes do COMMIT -> ROLLBACK | E07'
L 'Esperado: ROLLBACK; pendente e quantidade de atendimentos mantidos (17.000 e 1 antes e depois).'
Secao 'ANTES'
EstadoOcorrencia1
Secao 'CHAMADA com simularFalha=true (Development)'
Cliente 'Cliente: atender 1 2 EQ_ZEL_MOOCA_01 --simular-falha' @('atender', '1', '2', 'EQ_ZEL_MOOCA_01', $Executor, '--simular-falha') 1 | Out-Null
Secao 'DEPOIS'
EstadoOcorrencia1
Verificar 'Pendente continua 17.000 após ROLLBACK' ((SqlValor 'SELECT QUANTIDADEPENDENTE FROM OCORRENCIA WHERE ID=1') -eq 17)
Verificar 'Continua 1 atendimento (INSERT desfeito)' ((SqlValor 'SELECT COUNT(*) FROM ATENDIMENTO_OCORRENCIA') -eq 1)

# ================= T10 =================
Novo-Arquivo 'E08_05_T10_API_Parada_e_Retomada.txt' 'T10 - Parar somente a API durante teste do cliente; tentar de novo após reinício | E08'
L 'Esperado: erro de comunicação compreensível no cliente, sem sucesso aparente nem queda não tratada; retomada funciona.'
Secao 'Estado antes'
EstadoOcorrencia1
Secao 'Parar somente a API'
$sessaoTx = $script:Sessao
Parar-Api
Cliente 'Cliente com API parada: ocorrencias' @('ocorrencias') 2 | Out-Null
Cliente 'Cliente com API parada: atender 1 1 (operação de escrita)' @('atender', '1', '1', 'EQ_ZEL_MOOCA_01', $Executor) 2 | Out-Null
Secao 'Banco não foi alterado'
EstadoOcorrencia1
Verificar 'Pendente continua 17.000 (nenhuma escrita sem API)' ((SqlValor 'SELECT QUANTIDADEPENDENTE FROM OCORRENCIA WHERE ID=1') -eq 17)
Secao 'Reiniciar a API e tentar de novo'
Iniciar-Api | Out-Null
Cliente 'Cliente após reinício: ocorrencia 1' @('ocorrencia', '1') 0 | Out-Null

# Trechos de log da API (sessão das transações) anexados às evidências de COMMIT/ROLLBACK
$script:Arquivo = Join-Path $Ev 'E06_01_T07_COMMIT.txt'
Secao ("LOG DA API (testes\saida\api_sessao{0}.log) - SQL realmente executado na transação do T07" -f $sessaoTx)
Trecho-Log $sessaoTx 'simularFalha=False' '[TX] COMMIT'
$script:Arquivo = Join-Path $Ev 'E07_01_T08_ROLLBACK.txt'
Secao ("LOG DA API (testes\saida\api_sessao{0}.log) - UPDATE e INSERT executados, depois ROLLBACK" -f $sessaoTx)
Trecho-Log $sessaoTx 'simularFalha=True' '[TX] ROLLBACK'

# ================= Indisponibilidade do banco (C6) =================
Novo-Arquivo 'E08_06_Banco_Indisponivel_503.txt' 'C6/R7 - Banco indisponível: API sobe apontando para um banco inexistente e responde 503 tratado | E08'
L 'Esperado: 503 em ProblemDetails, sem stack trace nem string de conexão na resposta; API continua respondendo.'
Parar-Api
$porta2 = $Porta + 1
Iniciar-Api 'Server=(localdb)\MSSQLLocalDB;Database=GS_CSHARP_BANCO_INEXISTENTE;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5' $porta2 | Out-Null
Http 'GET /api/ocorrencias com banco indisponível' @("http://localhost:$porta2/api/ocorrencias") @(503) | Out-Null
Http 'POST atendimento com banco indisponível (nenhuma escrita possível)' @('-X', 'POST', "http://localhost:$porta2/api/ocorrencias/1/atendimentos", '-H', 'Content-Type: application/json', '--data-binary', "@$(Join-Path $Saida 'corpo_t10.json')") @(503) | Out-Null
$corpo503 = (& curl.exe -s "http://localhost:$porta2/api/ocorrencias")
Verificar 'Corpo do 503 não expõe string de conexão, nome do banco nem stack trace' (($corpo503 -notmatch 'Server=|BANCO_INEXISTENTE|   at ') -and ($corpo503 -match 'indispon'))
Http 'GET /api/equipe (não depende do banco) continua 200' @("http://localhost:$porta2/api/equipe") @(200) | Out-Null
Parar-Api
Secao 'API normal reiniciada ao final'
Iniciar-Api | Out-Null
Http 'GET /api/ocorrencias/1 (estado final)' @("$Base/api/ocorrencias/1") @(200) | Out-Null
Parar-Api

# ================= Resumo =================
Novo-Arquivo 'E08_00_Resumo_Verificacoes_Automaticas.txt' 'Resumo das verificações automáticas do roteiro (esperado x observado)'
$script:Resumo | Format-Table -AutoSize -Wrap | Out-String -Width 300 | ForEach-Object { L $_ }
$div = @($script:Resumo | Where-Object Situacao -eq 'DIVERGE').Count
L ("Total: {0} verificações | divergências: {1}" -f $script:Resumo.Count, $div)
