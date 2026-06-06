using System.Collections.Generic;
using System.Linq;

namespace Piscine.App.Coaching;

/// <summary>
/// Evenement emis par le shim <c>git</c> apres l'execution d'une commande : les arguments bruts
/// (<see cref="Argv"/>), le code de sortie et le dossier de travail. <b>Aucun parsing de stdout.</b>
/// Modele pur, agnostique au shell.
/// </summary>
public sealed record GitCommandEvent(IReadOnlyList<string> Argv, int ExitCode, string Cwd)
{
    /// <summary>
    /// Premier mot non-option de <see cref="Argv"/> (ex. <c>commit</c> dans <c>git -c x commit -m</c>),
    /// ou <c>null</c> si aucune sous-commande. On ignore les options (<c>-x</c>/<c>--x</c>) et leur
    /// valeur eventuelle pour <c>-c</c>/<c>-C</c> (git config / repertoire de travail).
    /// </summary>
    public string? Subcommand
    {
        get
        {
            for (var i = 0; i < Argv.Count; i++)
            {
                var token = Argv[i];
                if (token.Length == 0)
                {
                    continue;
                }

                if (token[0] != '-')
                {
                    return token;
                }

                // Options globales prenant une valeur : sauter l'argument suivant.
                if (token is "-c" or "-C")
                {
                    i++;
                }
            }

            return null;
        }
    }
}
