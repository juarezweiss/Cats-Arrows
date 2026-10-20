using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core.Localization;

namespace CatsAndArrows.UI
{
    public static class GameLocalization
    {
        public static LocalizationCatalog CreateCatalog()
        {
            var board = BoardLocalization.CreateCatalog();
            var extra = new Dictionary<string, string[]>
            {
                { "game.start", new[] { "Play", "Jogar", "Jugar" } },
                { "game.start.medium", new[] { "Start Medium", "Jogar no Médio", "Jugar en Medio" } },
                { "game.start.hard", new[] { "Start Hard", "Jogar no Difícil", "Jugar en Difícil" } },
                { "game.difficulty", new[] { "Difficulty", "Dificuldade", "Dificultad" } },
                { "game.difficulty.easy", new[] { "Easy", "Fácil", "Fácil" } },
                { "game.difficulty.medium", new[] { "Medium", "Médio", "Medio" } },
                { "game.difficulty.hard", new[] { "Hard", "Difícil", "Difícil" } },
                { "game.hint", new[] { "Hint", "Dica", "Pista" } },
                { "game.last-hint", new[] { "Last hint", "Última dica", "Última pista" } },
                { "game.clear", new[] { "Clear", "Limpar", "Limpiar" } },
                { "game.menu", new[] { "Menu", "Menu", "Menú" } },
                { "game.new", new[] { "Next board", "Próximo tabuleiro", "Siguiente tablero" } },
                { "game.back", new[] { "Back to the Game", "Voltar ao Jogo", "Volver al Juego" } },
                { "game.close", new[] { "Continue", "Continuar", "Continuar" } },
                { "game.retry", new[] { "Try Again", "Tentar Novamente", "Intentar de Nuevo" } },
                { "game.language", new[] { "Language", "Idioma", "Idioma" } },
                { "game.loading", new[] { "Finding a little puzzle…", "Preparando um tabuleiro…", "Preparando un tablero…" } },
                { "game.victory", new[] { "All paws found!", "Você encontrou todas as patas!", "¡Encontraste todas las patas!" } },
                { "completion.message", new[] { "you found all the paws", "você encontrou todas as patas", "encontraste todas las patas" } },
                { "completion.next", new[] { "next board", "próximo tabuleiro", "siguiente tablero" } },
                { "completion.back", new[] { "back to completed board", "voltar ao tabuleiro concluído", "volver al tablero completado" } },
                { "completion.menu", new[] { "menu", "menu", "menú" } },
                { "game.victory-body", new[] { "A lovely little moment of clarity. Take your time, or try another puzzle.", "Um pequeno momento de clareza. Aproveite a pausa ou experimente outro tabuleiro.", "Un pequeño momento de claridad. Tómate tu tiempo o prueba otro tablero." } },
                { "game.hint-target", new[] { "Row {row}, column {column}: {state}.", "Linha {row}, coluna {column}: {state}.", "Fila {row}, columna {column}: {state}." } },
                { "game.hint-correction", new[] { "This clue gently replaces your earlier mark.", "Esta pista substitui a marca anterior com tranquilidade.", "Esta pista reemplaza con calma tu marca anterior." } },
                { "game.hint-none", new[] { "There is no new clue to apply right now. You can keep exploring, or clear the marks and try again.", "Não há uma nova pista para aplicar agora. Você pode continuar explorando ou limpar as marcas e tentar novamente.", "Ahora no hay una nueva pista para aplicar. Puedes seguir explorando o limpiar las marcas y volver a intentarlo." } },
                { "game.failure", new[] { "Let's try another puzzle", "Vamos tentar outro tabuleiro", "Probemos otro tablero" } },
            };
            var locales = new[] { "en", "pt-BR", "es" };
            return new LocalizationCatalog(locales.Select((locale, index) =>
            {
                var messages = new Dictionary<string, string>(board.GetResource(locale).Messages);
                foreach (var entry in extra) messages.Add(entry.Key, entry.Value[index]);
                TutorialLocalization.AddMessages(messages, index);
                MonetizationLocalization.AddMessages(messages, index);
                return new LocaleResource(locale, messages);
            }));
        }
    }
}
