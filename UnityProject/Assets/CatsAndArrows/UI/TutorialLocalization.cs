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
                "Welcome to Paws! Tap Continue to begin.",
                "Boas-vindas a Paws! Toque em Continuar para começar.",
                "¡Te damos la bienvenida a Paws! Toca Continuar."
            } },
            { "tutorial.goal", new[] {
                "Find every paw! Numbers beside each row and above each column show how many paws belong there.",
                "Encontre as patas! Os números ao lado das linhas e acima das colunas mostram quantas patas há ali.",
                "¡Encuentra las patas! Los números junto a las filas y sobre las columnas dicen cuántas patas hay allí."
            } },
            { "tutorial.begin", new[] {
                "Let's find the paws together, one little step at a time.",
                "Vamos encontrar as patas juntos, um passo de cada vez.",
                "Busquemos las patas juntos, paso a paso."
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
                "<b>X</b>: this cell does not contain a paw.",
                "<b>X</b>: esta célula não contém uma pata.",
                "<b>X</b>: esta celda no contiene una pata."
            } },
            { "tutorial.cat-state", new[] {
                "<b>Paw</b>: this cell contains a paw.",
                "<b>Pata</b>: esta célula contém uma pata.",
                "<b>Pata</b>: esta celda contiene una pata."
            } },
            { "tutorial.hatched", new[] {
                "Hatched cells have no paws and cannot be changed.",
                "As células hachuradas não têm patas e não podem ser alteradas.",
                "Las celdas rayadas no tienen patas y no se pueden cambiar."
            } },
            { "tutorial.arrows", new[] {
                "Arrows cannot be changed. They point toward at least one paw.",
                "As setas não podem ser alteradas. Apontam para pelo menos uma pata.",
                "Las flechas no se pueden cambiar. Apuntan hacia al menos una pata."
            } },
            { "tutorial.practice", new[] {
                "Tap the highlighted cell 3 times: Empty → X → Paw → Empty.",
                "Toque 3 vezes na célula destacada: Vazia → X → Pata → Vazia.",
                "Toca 3 veces la celda resaltada: Vacía → X → Pata → Vacía."
            } },
            { "tutorial.zero-column", new[] {
                "This column has 0 paws. Swipe its empty cells to mark X.",
                "Esta coluna tem 0 patas. Arraste pelas células vazias para marcar X.",
                "Esta columna tiene 0 patas. Desliza por las celdas vacías para marcar X."
            } },
            { "tutorial.zero-row", new[] {
                "This row has 0 paws. Swipe its empty cells to mark X.",
                "Esta linha tem 0 patas. Arraste pelas células vazias para marcar X.",
                "Esta fila tiene 0 patas. Desliza por las celdas vacías para marcar X."
            } },
            { "tutorial.first-cat", new[] {
                "This arrow’s only space must be a paw. Tap it twice.",
                "Só há uma célula à frente da seta. Toque 2 vezes para uma pata.",
                "Solo hay una celda delante de la flecha. Toca 2 veces para una pata."
            } },
            { "tutorial.first-column-x", new[] {
                "This column's 1 paw is found. Swipe its empty cells to X.",
                "A coluna já tem sua única pata. Arraste pelas vazias para X.",
                "La columna ya tiene su pata. Desliza por las vacías para marcar X."
            } },
            { "tutorial.second-cat", new[] {
                "This arrow also has just one space ahead. Tap it twice for a paw.",
                "Outra seta com só uma célula à frente. Toque 2 vezes para uma pata.",
                "Otra flecha con una sola celda delante. Toca 2 veces para una pata."
            } },
            { "tutorial.second-row-x", new[] {
                "This row's 1 paw is found. Swipe empty cells to X; the paw stays.",
                "Esta linha já tem sua única pata. Arraste pelas vazias para X; a pata fica.",
                "La fila ya tiene su pata. Desliza por las vacías para X; la pata queda."
            } },
            { "tutorial.diagonal-cat", new[] {
                "Follow the 45° arrow. Only this cell can have a paw. Tap twice.",
                "Siga a seta a 45°. Só esta célula pode ter pata. Toque 2 vezes.",
                "Sigue la flecha a 45°. Solo esta celda puede tener pata. Toca 2 veces."
            } },
            { "tutorial.fourth-cat", new[] {
                "This row needs 2 paws. One is found; only this cell remains. Tap twice.",
                "Esta linha pede 2 patas. Já há uma; só resta esta célula. Toque 2 vezes.",
                "Esta fila pide 2 patas. Ya hay una; solo queda esta celda. Toca 2 veces."
            } },
            { "tutorial.single-x", new[] {
                "This column has its 1 paw. Tap the remaining empty cell once for X.",
                "Esta coluna já tem sua única pata. Toque uma vez na vazia restante para X.",
                "Esta columna ya tiene su pata. Toca la última vacía una vez para X."
            } },
            { "tutorial.final-cat", new[] {
                "This column needs 2 paws. One is found. Tap twice for the last paw.",
                "Esta coluna pede 2 patas. Já há uma. Toque 2 vezes para a última.",
                "Esta columna pide 2 patas. Ya hay una. Toca 2 veces para la última."
            } },
            { "tutorial.independent", new[] {
                "Now try it yourself! Find all the paws on this same board.",
                "Agora é sua vez! Encontre todas as patas neste mesmo tabuleiro.",
                "¡Ahora te toca! Encuentra todas las patas en este mismo tablero."
            } },
            { "tutorial.complete", new[] {
                "All paws found! Ready for a random puzzle?",
                "Você encontrou todas as patas! Vamos jogar um tabuleiro aleatório?",
                "¡Encontraste todas las patas! ¿Jugamos un tablero aleatorio?"
            } }
        };

        internal static void AddMessages(IDictionary<string, string> messages, int localeIndex)
        {
            foreach (var entry in Messages) messages.Add(entry.Key, entry.Value[localeIndex]);
        }
    }
}
