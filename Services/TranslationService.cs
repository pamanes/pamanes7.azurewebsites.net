using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Blog.Services
{
    public static class TranslationService
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Create"] = "Create",
                ["Title"] = "Title",
                ["Subtitle"] = "Title",
                ["Search"] = "Search",
                ["Privacy"] = "Privacy",
                ["Edit"] = "Edit",
                ["Save"] = "Save",
                ["TagsCommaSeparated"] = "Tags (comma‑separated)",
                ["LatestPosts"] = "Latest Posts",
                ["PostedOn"] = "Posted On",
                ["CreateMarkdownPost"] = "Create Post",
                ["EditMarkdownPost"] = "Edit Post",
                ["Tags"] = "Tags",
                /*Policy*/
                ["PrivacyPolicy"] = "Privacy Policy",
                ["EffectiveDate"] = "Effective Date",
                ["IntroTitle"] = "1. Introduction",
                ["IntroText"] = "Welcome to TinyBlog. Your privacy is important to us. This privacy policy explains what information we collect, how we use it, and your rights regarding your data.",
                ["NoDataTitle"] = "2. Information We Do Not Collect",
                ["NoDataText"] = "We do not collect any personal data from visitors who browse the site:",
                ["NoTrackingCookies"] = "No tracking cookies",
                ["NoAnalytics"] = "No third-party analytics",
                ["NoForms"] = "No forms or submissions that collect personal info",
                ["AnonymousBrowsing"] = "You are free to browse, read, and interact with public content anonymously.",
                ["CookiesTitle"] = "3. Cookies",
                ["CookiesText"] = "We use a single, essential cookie for admin login only. This cookie is required for authentication and session management when logging into the blog management area.",
                ["CookiesVisitors"] = "Visitors who are not admins will not receive cookies from this site.",
                ["MediaLinksTitle"] = "4. Embedded Media & External Links",
                ["MediaLinksText"] = "Posts may contain embedded images or links to external websites. TinyBlog is not responsible for the privacy practices or content of third-party sites.",
                ["ChangesTitle"] = "5. Changes to This Policy",
                ["ChangesText"] = "We may update this privacy policy occasionally. Any changes will be posted on this page with an updated effective date.",
                ["ContactTitle"] = "6. Contact",
                ["ContactText"] = "If you have questions about this policy, please contact the site administrator.",
                ["AntiForgeryNote"] = "We also use anti-forgery protection to secure admin form submissions.",
                /*Cookie Policy*/
                ["CookieConsentMessage"] = "This site uses a cookie for admin login and an anti-forgery token to protect admin forms. By continuing, you accept these cookies.",
                ["CookieConsentOk"] = "OK"
            },
            ["es"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Create"] = "Crear",
                ["Title"] = "Título",
                ["Subtitle"] = "Subtítulo",
                ["TagsCommaSeparated"] = "Etiquetas (separadas por comas)",
                ["Search"] = "Buscar",
                ["Privacy"] = "Privacidad",
                ["Edit"] = "Editar",
                ["Save"] = "Guardar",
                ["LatestPosts"] = "Últimas Publicaciones",
                ["PostedOn"] = "Publicado el",
                ["CreateMarkdownPost"] = "Crear Publicación",
                ["EditMarkdownPost"] = "Editar Publicación",
                ["Tags"] = "Etiquetas",
                /*Policy*/
                ["PrivacyPolicy"] = "Política de Privacidad",
                ["EffectiveDate"] = "Fecha de Vigencia",
                ["IntroTitle"] = "1. Introducción",
                ["IntroText"] = "Bienvenido a TinyBlog. Su privacidad es importante para nosotros. Esta política explica qué información recopilamos, cómo la usamos y sus derechos.",
                ["NoDataTitle"] = "2. Información que No Recopilamos",
                ["NoDataText"] = "No recopilamos ningún dato personal de los visitantes del sitio:",
                ["NoTrackingCookies"] = "Sin cookies de seguimiento",
                ["NoAnalytics"] = "Sin analíticas de terceros",
                ["NoForms"] = "Sin formularios ni envíos que recopilen información personal",
                ["AnonymousBrowsing"] = "Puede navegar y leer el contenido público de forma anónima.",
                ["CookiesTitle"] = "3. Cookies",
                ["CookiesText"] = "Usamos una sola cookie esencial para el inicio de sesión de administradores. Esta cookie es necesaria para la autenticación y gestión de sesión.",
                ["CookiesVisitors"] = "Los visitantes que no son administradores no recibirán cookies de este sitio.",
                ["MediaLinksTitle"] = "4. Contenido Externo y Enlaces",
                ["MediaLinksText"] = "Las publicaciones pueden contener imágenes incrustadas o enlaces externos. TinyBlog no se hace responsable del contenido o prácticas de privacidad de sitios externos.",
                ["ChangesTitle"] = "5. Cambios en Esta Política",
                ["ChangesText"] = "Esta política puede actualizarse ocasionalmente. Los cambios se publicarán en esta página con la nueva fecha de vigencia.",
                ["ContactTitle"] = "6. Contacto",
                ["ContactText"] = "Si tiene preguntas sobre esta política, contacte al administrador del sitio.",
                ["AntiForgeryNote"] = "También usamos protección contra falsificación para asegurar los formularios del área de administración.",
                /*Cookie Policy*/
                ["CookieConsentMessage"] = "Este sitio utiliza una cookie para el inicio de sesión de administradores y un token antifalsificación para proteger los formularios administrativos. Al continuar, aceptas estas cookies.",
                ["CookieConsentOk"] = "Aceptar"
            },
            // Add more languages here...
        };

        public static string T(HttpContext httpContext, string key)
        {
            var lang = httpContext.Request.Headers["Accept-Language"].ToString()?.Split(',').FirstOrDefault()?.Trim().Substring(0, 2) ?? "en";

            if (!_translations.TryGetValue(lang, out var langDict))
            {
                langDict = _translations["en"];
            }

            return langDict.TryGetValue(key, out var value) ? value : key;
        }
    }

}
