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
                { "game.start", new[] { "Start Easy", "Jogar no Fácil", "Jugar en Fácil" } },
                { "game.hint", new[] { "Hint", "Dica", "Pista" } },
                { "game.last-hint", new[] { "Last hint", "Última dica", "Última pista" } },
                { "game.clear", new[] { "Clear", "Limpar", "Limpiar" } },
                { "game.menu", new[] { "Menu", "Menu", "Menú" } },
                { "game.new", new[] { "New Game", "Novo Jogo", "Nuevo Juego" } },
                { "game.back", new[] { "Back to the Game", "Voltar ao Jogo", "Volver al Juego" } },
                { "game.close", new[] { "Continue", "Continuar", "Continuar" } },
                { "game.retry", new[] { "Try Again", "Tentar Novamente", "Intentar de Nuevo" } },
                { "game.rules", new[] { "How to Play", "Como Jogar", "Cómo Jugar" } },
                { "game.language", new[] { "Language", "Idioma", "Idioma" } },
                { "game.loading", new[] { "Finding a little puzzle…", "Preparando um tabuleiro…", "Preparando un tablero…" } },
                { "game.victory", new[] { "All cats found!", "Você encontrou todos os gatos!", "¡Encontraste todos los gatos!" } },
                { "game.victory-body", new[] { "A lovely little moment of clarity. Take your time, or try another puzzle.", "Um pequeno momento de clareza. Aproveite a pausa ou experimente outro tabuleiro.", "Un pequeño momento de claridad. Tómate tu tiempo o prueba otro tablero." } },
                { "game.hint-target", new[] { "Row {row}, column {column}: {state}.", "Linha {row}, coluna {column}: {state}.", "Fila {row}, columna {column}: {state}." } },
                { "game.hint-correction", new[] { "This clue gently replaces your earlier mark.", "Esta pista substitui a marca anterior com tranquilidade.", "Esta pista reemplaza con calma tu marca anterior." } },
                { "game.hint-none", new[] { "There is no new clue to apply right now. You can keep exploring, or clear the marks and try again.", "Não há uma nova pista para aplicar agora. Você pode continuar explorando ou limpar as marcas e tentar novamente.", "Ahora no hay una nueva pista para aplicar. Puedes seguir explorando o limpiar las marcas y volver a intentarlo." } },
                { "game.failure", new[] { "Let's try another puzzle", "Vamos tentar outro tabuleiro", "Probemos otro tablero" } },
                { "game.rules-body", new[] {
                    "Find every cat using the clues.\n\nNumbers beside each row and above each column show exactly how many cats belong there.\n\nEach arrow points to at least one cat along its path. Diagonal arrows follow an exact 45° line.\n\nTap a cell to cycle Empty → X → Cat → Empty. X means no cat. Drag horizontally or vertically from an empty cell to mark empty cells with X.\n\nArrow cells cannot be changed. Hatched cells are protected: an arrow's clue already proves there is no cat there.\n\nYou finish as soon as all cats are correctly placed. The other cells can be empty or X. There is no timer or penalty for exploring.\n\nHints are unlimited and immediately place one correct Cat or X. Clear removes your marks. New Game starts a fresh Easy puzzle. Menu lets you read these rules or change language; your current game stays here while the app is open.",
                    "Encontre todos os gatos usando as pistas.\n\nOs números ao lado de cada linha e acima de cada coluna mostram exatamente quantos gatos devem estar ali.\n\nCada seta aponta para pelo menos um gato ao longo de seu caminho. Setas diagonais seguem uma linha exata de 45°.\n\nToque em uma célula para alternar Vazia → X → Gato → Vazia. X significa sem gato. Arraste na horizontal ou vertical a partir de uma célula vazia para marcar células vazias com X.\n\nAs células com setas não podem ser alteradas. Células hachuradas são protegidas: a pista de uma seta já prova que não há gato ali.\n\nVocê termina assim que todos os gatos estiverem nos lugares corretos. As outras células podem estar vazias ou com X. Não há cronômetro nem penalidade por explorar.\n\nAs dicas são ilimitadas e colocam imediatamente um Gato ou X correto. Limpar remove suas marcas. Novo Jogo começa um novo tabuleiro Fácil. O Menu permite ler estas regras ou mudar o idioma; o jogo atual fica aqui enquanto o aplicativo está aberto.",
                    "Encuentra todos los gatos usando las pistas.\n\nLos números al lado de cada fila y encima de cada columna indican exactamente cuántos gatos debe haber allí.\n\nCada flecha apunta a por lo menos un gato a lo largo de su recorrido. Las flechas diagonales siguen una línea exacta de 45°.\n\nToca una celda para alternar Vacía → X → Gato → Vacía. X significa sin gato. Arrastra en horizontal o vertical desde una celda vacía para marcar celdas vacías con X.\n\nLas celdas con flechas no se pueden cambiar. Las celdas rayadas están protegidas: la pista de una flecha ya demuestra que allí no hay gato.\n\nTerminas cuando todos los gatos están en sus lugares correctos. Las demás celdas pueden estar vacías o con X. No hay reloj ni penalización por explorar.\n\nLas pistas son ilimitadas y colocan inmediatamente un Gato o X correcto. Limpiar elimina tus marcas. Nuevo Juego comienza un nuevo tablero Fácil. El Menú permite leer estas reglas o cambiar el idioma; tu partida actual se mantiene mientras la aplicación está abierta."
                } }
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
