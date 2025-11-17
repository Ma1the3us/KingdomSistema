using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;



namespace MeuProjetoMVC.Autenticacao
{
    /// <summary>
    /// Filtro que exige que o usuário esteja logado
    /// e (opcionalmente) possua uma das roles especificadas.
    /// Exemplo de uso:
    /// [SessionAuthorize(RoleAnyOf = "Admin,Funcionario")]
    /// </summary>
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Roles permitidas, separadas por vírgula. Ex: "Admin,Bibliotecario"
        /// </summary>
        public string? RoleAnyOf { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var role = http.Session.GetString(SessionKeys.UserRole);
            var userId = http.Session.GetInt32(SessionKeys.UserId);

            // 🔒 Verifica se existe sessão válida
            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // 🔒 Se roles foram definidas, verifica se a role atual está entre elas
            if (!string.IsNullOrWhiteSpace(RoleAnyOf))
            {
                var allowed = RoleAnyOf
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (string.IsNullOrWhiteSpace(role) || !allowed.Contains(role))
                {
                    context.Result = new RedirectToActionResult("AcessoNegado", "Auth", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
