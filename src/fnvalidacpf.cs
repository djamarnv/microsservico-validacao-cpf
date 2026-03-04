using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsservico.ValidacaoCpf
{
    public static class fnvalidacpf
    {
        [FunctionName("fnvalidacpf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Iniciando validação de CPF...");

            // 1) Lê de query (?cpf=...) ou do corpo JSON { "cpf": "..." }
            string cpf = req.Query["cpf"];
            if (string.IsNullOrWhiteSpace(cpf))
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        dynamic data = JsonConvert.DeserializeObject(body);
                        cpf = data?.cpf;
                    }
                    catch { /* ignora erro de parse e segue adiante */ }
                }
            }

            // 2) Entrada obrigatória
            if (string.IsNullOrWhiteSpace(cpf))
                return new BadRequestObjectResult(new
                {
                    ok = false,
                    error = "Informe o CPF via ?cpf= na URL ou no corpo JSON { \"cpf\": \"...\" }"
                });

            // 3) Validação
            bool valido = CpfValido(cpf);

            // 4) Resposta
            return new OkObjectResult(new
            {
                ok = true,
                cpf,
                valido
            });
        }

        /// <summary>
        /// Validação de CPF:
        /// - remove caracteres não numéricos
        /// - recusa CPFs com todos dígitos iguais
        /// - calcula dígitos verificadores (método oficial)
        /// </summary>
        private static bool CpfValido(string cpf)
        {
            cpf = Regex.Replace(cpf ?? string.Empty, "[^0-9]", "");

            if (cpf.Length != 11) return false;

            // recusa sequências (000..., 111..., etc.)
            if (new string(cpf[0], 11) == cpf) return false;

            int[] m1 = {10,9,8,7,6,5,4,3,2};
            int[] m2 = {11,10,9,8,7,6,5,4,3,2};

            // primeiro DV
            int soma = 0;
            for (int i = 0; i < 9; i++)
                soma += (cpf[i] - '0') * m1[i];
            int resto = soma % 11;
            int d1 = resto < 2 ? 0 : 11 - resto;

            // segundo DV
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += (cpf[i] - '0') * m2[i];
            resto = soma % 11;
            int d2 = resto < 2 ? 0 : 11 - resto;

            return cpf.EndsWith($"{d1}{d2}");
        }
    }
}
