using System.Collections.Generic;
using System.Linq;
using CatsAndArrows.Core.Localization;

namespace CatsAndArrows.UI
{
    public static class BoardLocalization
    {
        public static LocalizationCatalog CreateCatalog()
        {
            var core = DefaultLocalization.CreateCatalog();
            var extras = new Dictionary<string, string[]>
            {
                { "board.title", new[] { "Cats & Arrows", "Gatos e Setas", "Gatos y Flechas" } },
                { "board.easy", new[] { "Easy · 5 × 5", "Fácil · 5 × 5", "Fácil · 5 × 5" } },
                { "board.intro", new[] { "A little space to think.", "Um pequeno espaço para pensar.", "Un pequeño espacio para pensar." } },
                { "board.review", new[] { "Board preview", "Prévia do tabuleiro", "Vista previa del tablero" } },
                { "board.symbols", new[] { "Symbols", "Símbolos", "Símbolos" } },
                { "board.empty", new[] { "Empty", "Vazia", "Vacía" } },
                { "board.x", new[] { "No cat", "Sem gato", "Sin gato" } },
                { "board.cat", new[] { "Cat", "Gato", "Gato" } },
                { "board.arrow", new[] { "Arrow", "Seta", "Flecha" } },
                { "board.protected", new[] { "Protected", "Protegida", "Protegida" } },
                { "board.completed", new[] { "Puzzle complete", "Tabuleiro completo", "Tablero completo" } },
                { "board.cell-description", new[] { "Row {row}, column {column}: {state}", "Linha {row}, coluna {column}: {state}", "Fila {row}, columna {column}: {state}" } },
                { "board.arrow-description", new[] { "Arrow pointing {direction}", "Seta apontando {direction}", "Flecha apuntando {direction}" } },
                { "board.row-total", new[] { "Row {index}: {total} cats", "Linha {index}: {total} gatos", "Fila {index}: {total} gatos" } },
                { "board.column-total", new[] { "Column {index}: {total} cats", "Coluna {index}: {total} gatos", "Columna {index}: {total} gatos" } }
            };
            var locales = new[] { "en", "pt-BR", "es" };
            return new LocalizationCatalog(locales.Select((locale, index) =>
            {
                var messages = new Dictionary<string, string>(core.GetResource(locale).Messages);
                foreach (var extra in extras) messages.Add(extra.Key, extra.Value[index]);
                return new LocaleResource(locale, messages);
            }));
        }
    }
}
