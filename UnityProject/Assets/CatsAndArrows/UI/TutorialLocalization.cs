using System.Collections.Generic;

namespace CatsAndArrows.UI
{
    /// <summary>Issue #26: short, fixed-board lessons, in catalog locale order en / pt-BR / es.</summary>
    internal static class TutorialLocalization
    {
        private static readonly Dictionary<string, string[]> Messages = new Dictionary<string, string[]>
        {
            { "tutorial.title", new[] { "Tutorial", "Tutorial", "Tutorial" } },
            { "tutorial.continue", new[] { "Continue", "Continuar", "Continuar" } },
            { "tutorial.start", new[] { "Let's start", "Vamos começar", "Empecemos" } },
            { "tutorial.skip", new[] { "Skip", "Pular", "Omitir" } },
            { "tutorial.play", new[] { "Play", "Jogar", "Jugar" } },
            { "tutorial.welcome", new[] {
                "Welcome to Cats & Arrows! Tap Continue to begin.",
                "Boas-vindas a Gatos e Setas! Toque em Continuar para começar.",
                "¡Te damos la bienvenida a Gatos y Flechas! Toca Continuar."
            } },
            { "tutorial.goal", new[] {
                "Find every cat! Numbers beside each row and above each column show how many cats belong there.",
                "Encontre os gatos! Os números ao lado das linhas e acima das colunas mostram quantos gatos há ali.",
                "¡Encuentra los gatos! Los números junto a las filas y sobre las columnas dicen cuántos gatos hay allí."
            } },
            { "tutorial.begin", new[] {
                "Let's find the cats together, one little step at a time.",
                "Vamos encontrar os gatos juntos, um passo de cada vez.",
                "Busquemos los gatos juntos, paso a paso."
            } },
            { "tutorial.states-intro", new[] {
                "Empty cells are editable and have three possible states.",
                "As células vazias podem ser alteradas e têm três estados possíveis.",
                "Las celdas vacías se pueden cambiar y tienen tres estados posibles."
            } },
            { "tutorial.empty-state", new[] {
                "<b>Empty</b>: undecided.",
                "<b>Vazia</b>: sem decisão.",
                "<b>Vacía</b>: sin decidir."
            } },
            { "tutorial.x-state", new[] {
                "<b>X</b>: this cell does not contain a cat.",
                "<b>X</b>: esta célula não contém um gato.",
                "<b>X</b>: esta celda no contiene un gato."
            } },
            { "tutorial.cat-state", new[] {
                "<b>Cat</b>: this cell contains a cat.",
                "<b>Gato</b>: esta célula contém um gato.",
                "<b>Gato</b>: esta celda contiene un gato."
            } },
            { "tutorial.hatched", new[] {
                "Hatched cells have no cats and cannot be changed.",
                "As células hachuradas não têm gatos e não podem ser alteradas.",
                "Las celdas rayadas no tienen gatos y no se pueden cambiar."
            } },
            { "tutorial.arrows", new[] {
                "Arrows cannot be changed. They point toward at least one cat.",
                "As setas não podem ser alteradas. Apontam para pelo menos um gato.",
                "Las flechas no se pueden cambiar. Apuntan hacia al menos un gato."
            } },
            { "tutorial.practice", new[] {
                "Tap the highlighted cell 3 times: Empty → X → Cat → Empty.",
                "Toque 3 vezes na célula destacada: Vazia → X → Gato → Vazia.",
                "Toca 3 veces la celda resaltada: Vacía → X → Gato → Vacía."
            } },
            { "tutorial.zero-column", new[] {
                "This column has 0 cats. Swipe its empty cells to mark X.",
                "Esta coluna tem 0 gatos. Arraste pelas células vazias para marcar X.",
                "Esta columna tiene 0 gatos. Desliza por las celdas vacías para marcar X."
            } },
            { "tutorial.zero-row", new[] {
                "This row has 0 cats. Swipe its empty cells to mark X.",
                "Esta linha tem 0 gatos. Arraste pelas células vazias para marcar X.",
                "Esta fila tiene 0 gatos. Desliza por las celdas vacías para marcar X."
            } },
            { "tutorial.first-cat", new[] {
                "This arrow’s only space must be a cat. Tap it twice.",
                "Só há uma célula à frente da seta. Toque 2 vezes para um gato.",
                "Solo hay una celda delante de la flecha. Toca 2 veces para un gato."
            } },
            { "tutorial.first-column-x", new[] {
                "This column's 1 cat is found. Swipe its empty cells to X.",
                "A coluna já tem seu único gato. Arraste pelas vazias para X.",
                "La columna ya tiene su gato. Desliza por las vacías para marcar X."
            } },
            { "tutorial.second-cat", new[] {
                "This arrow also has just one space ahead. Tap it twice for a cat.",
                "Outra seta com só uma célula à frente. Toque 2 vezes para um gato.",
                "Otra flecha con una sola celda delante. Toca 2 veces para un gato."
            } },
            { "tutorial.second-row-x", new[] {
                "This row's 1 cat is found. Swipe empty cells to X; the cat stays.",
                "Esta linha já tem seu único gato. Arraste pelas vazias para X; o gato fica.",
                "La fila ya tiene su gato. Desliza por las vacías para X; el gato queda."
            } },
            { "tutorial.diagonal-cat", new[] {
                "Follow the 45° arrow. Only this cell can have a cat. Tap twice.",
                "Siga a seta a 45°. Só esta célula pode ter gato. Toque 2 vezes.",
                "Sigue la flecha a 45°. Solo esta celda puede tener gato. Toca 2 veces."
            } },
            { "tutorial.fourth-cat", new[] {
                "This row needs 2 cats. One is found; only this cell remains. Tap twice.",
                "Esta linha pede 2 gatos. Já há um; só resta esta célula. Toque 2 vezes.",
                "Esta fila pide 2 gatos. Ya hay uno; solo queda esta celda. Toca 2 veces."
            } },
            { "tutorial.single-x", new[] {
                "This column has its 1 cat. Tap the remaining empty cell once for X.",
                "Esta coluna já tem seu único gato. Toque uma vez na vazia restante para X.",
                "Esta columna ya tiene su gato. Toca la última vacía una vez para X."
            } },
            { "tutorial.final-cat", new[] {
                "This column needs 2 cats. One is found. Tap twice for the last cat.",
                "Esta coluna pede 2 gatos. Já há um. Toque 2 vezes para o último.",
                "Esta columna pide 2 gatos. Ya hay uno. Toca 2 veces para el último."
            } },
            { "tutorial.independent", new[] {
                "Now try it yourself! Find all the cats on this same board.",
                "Agora é sua vez! Encontre todos os gatos neste mesmo tabuleiro.",
                "¡Ahora te toca! Encuentra todos los gatos en este mismo tablero."
            } },
            { "tutorial.complete", new[] {
                "All cats found! Ready for a random puzzle?",
                "Você encontrou todos os gatos! Vamos jogar um tabuleiro aleatório?",
                "¡Encontraste todos los gatos! ¿Jugamos un tablero aleatorio?"
            } }
        };

        internal static void AddMessages(IDictionary<string, string> messages, int localeIndex)
        {
            foreach (var entry in Messages) messages.Add(entry.Key, entry.Value[localeIndex]);
        }
    }
}
