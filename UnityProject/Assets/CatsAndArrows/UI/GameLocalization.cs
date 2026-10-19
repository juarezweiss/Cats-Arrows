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
                { "game.rules", new[] { "How to Play", "Como Jogar", "Cómo Jugar" } },
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
                { "game.rules-body", new[] {
                    "Find every paw using the clues.\n\nNumbers beside each row and above each column show exactly how many paws belong there.\n\nEach arrow points to at least one paw along its path. Diagonal arrows follow an exact 45° line.\n\nTap a cell to cycle Empty → X → Paw → Empty. X means no paw. Drag horizontally or vertically from an empty cell to mark empty cells with X.\n\nArrow cells cannot be changed. On an ordinary board, hold an arrow for 350 ms to outline its path until you release. Hatched cells are protected: an arrow's clue already proves there is no paw there.\n\nYou finish as soon as all paws are correctly placed. The other cells can be empty or X. There is no timer or penalty for exploring.\n\nHints are unlimited and immediately place one correct Paw or X. Clear removes your marks. Next board starts another puzzle with a random difficulty. The same difficulty appears at most twice in a row. Menu lets you read these rules or change language; your current game stays here while the app is open.",
                    "Encontre todas as patas usando as pistas.\n\nOs números ao lado de cada linha e acima de cada coluna mostram exatamente quantas patas devem estar ali.\n\nCada seta aponta para pelo menos uma pata ao longo de seu caminho. Setas diagonais seguem uma linha exata de 45°.\n\nToque em uma célula para alternar Vazia → X → Pata → Vazia. X significa sem pata. Arraste na horizontal ou vertical a partir de uma célula vazia para marcar células vazias com X.\n\nAs células com setas não podem ser alteradas. Em um tabuleiro comum, segure uma seta por 350 ms para destacar seu caminho até soltar. Células hachuradas são protegidas: a pista de uma seta já prova que não há pata ali.\n\nVocê termina assim que todas as patas estiverem nos lugares corretos. As outras células podem estar vazias ou com X. Não há cronômetro nem penalidade por explorar.\n\nAs dicas são ilimitadas e colocam imediatamente uma marca correta de Pata ou X. Limpar remove suas marcas. Próximo tabuleiro começa outro jogo com uma dificuldade aleatória. A mesma dificuldade aparece no máximo duas vezes seguidas. O Menu permite ler estas regras ou mudar o idioma; o jogo atual fica aqui enquanto o aplicativo está aberto.",
                    "Encuentra todas las patas usando las pistas.\n\nLos números al lado de cada fila y encima de cada columna indican exactamente cuántas patas debe haber allí.\n\nCada flecha apunta a por lo menos una pata a lo largo de su recorrido. Las flechas diagonales siguen una línea exacta de 45°.\n\nToca una celda para alternar Vacía → X → Pata → Vacía. X significa sin pata. Arrastra en horizontal o vertical desde una celda vacía para marcar celdas vacías con X.\n\nLas celdas con flechas no se pueden cambiar. En un tablero normal, mantén pulsada una flecha durante 350 ms para resaltar su recorrido hasta soltarla. Las celdas rayadas están protegidas: la pista de una flecha ya demuestra que allí no hay pata.\n\nTerminas cuando todas las patas están en sus lugares correctos. Las demás celdas pueden estar vacías o con X. No hay reloj ni penalización por explorar.\n\nLas pistas son ilimitadas y colocan inmediatamente una marca correcta de Pata o X. Limpiar elimina tus marcas. Siguiente tablero comienza otro juego con una dificultad aleatoria. La misma dificultad aparece como máximo dos veces seguidas. El Menú permite leer estas reglas o cambiar el idioma; tu partida actual se mantiene mientras la aplicación está abierta."
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
