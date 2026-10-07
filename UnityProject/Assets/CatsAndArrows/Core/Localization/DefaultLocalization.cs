using System.Collections.Generic;

namespace CatsAndArrows.Core.Localization
{
    /// <summary>Initial English, Brazilian Portuguese and generic Spanish resources for Issue #3.
    /// Additional locales are registered as LocaleResource data, without changes to puzzle logic.</summary>
    public static class DefaultLocalization
    {
        public static LocalizationCatalog CreateCatalog() => new LocalizationCatalog(new[]
        {
            new LocaleResource("en", new Dictionary<string, string>
            {
                { "line.row", "Row" }, { "line.column", "Column" },
                { "direction.up", "up" }, { "direction.down", "down" }, { "direction.left", "left" }, { "direction.right", "right" },
                { "deduction.zero-line", "{line} {index} needs no cats. Its remaining cells cannot contain cats." },
                { "deduction.line-satisfied", "{line} {index} already has its required cats ({total}). Its remaining cells cannot contain cats." },
                { "deduction.fill-remaining-line", "{line} {index} needs a cat in each remaining cell ({count} remaining)." },
                { "deduction.arrow-singleton", "The arrow pointing {direction} at row {row}, column {column} has only one possible cat cell left. That cell must contain a cat." },
                { "deduction.arrow-line-exclusion", "The arrow at row {row}, column {column}, pointing {direction}, requires the last cat in {line} {index} to lie in its direction. Cells outside that segment cannot contain cats." },
                { "solver.solved", "Solved using deductions." }, { "solver.stalled", "No further deduction is available." },
                { "solver.contradiction", "These clues cannot all be satisfied." }, { "solver.invalid-input", "The puzzle clues are invalid." },
                { "solver.budget-exceeded", "The deduction limit was reached." },
                { "uniqueness.unique", "The puzzle has exactly one solution." }, { "uniqueness.multiple", "The puzzle has more than one solution." },
                { "uniqueness.no-solution", "The puzzle has no solution." }, { "uniqueness.invalid-input", "The puzzle clues are invalid." },
                { "uniqueness.incomplete", "The solution check is incomplete." },
                { "control.hint", "Hint" }, { "control.clear", "Clear" }, { "control.menu", "Menu" },
                { "control.new-game", "New Game" }, { "control.back-to-game", "Back to the Game" },
                { "settings.language", "Language" }, { "settings.device-language", "Device language" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            }),
            new LocaleResource("pt-BR", new Dictionary<string, string>
            {
                { "line.row", "Linha" }, { "line.column", "Coluna" },
                { "direction.up", "para cima" }, { "direction.down", "para baixo" }, { "direction.left", "para a esquerda" }, { "direction.right", "para a direita" },
                { "deduction.zero-line", "{line} {index} não precisa de gatos. As células restantes não podem conter gatos." },
                { "deduction.line-satisfied", "{line} {index} já tem os gatos necessários ({total}). As células restantes não podem conter gatos." },
                { "deduction.fill-remaining-line", "{line} {index} precisa de um gato em cada célula restante (restantes: {count})." },
                { "deduction.arrow-singleton", "A seta apontando {direction} na linha {row}, coluna {column} tem apenas uma célula possível para um gato. Essa célula deve conter um gato." },
                { "deduction.arrow-line-exclusion", "A seta na linha {row}, coluna {column}, apontando {direction}, exige que o último gato da {line} {index} esteja na direção indicada. As células fora desse segmento não podem conter gatos." },
                { "solver.solved", "Resolvido usando deduções." }, { "solver.stalled", "Nenhuma outra dedução está disponível." },
                { "solver.contradiction", "Estas pistas não podem ser todas satisfeitas." }, { "solver.invalid-input", "As pistas do quebra-cabeça são inválidas." },
                { "solver.budget-exceeded", "O limite de deduções foi atingido." },
                { "uniqueness.unique", "O quebra-cabeça tem exatamente uma solução." }, { "uniqueness.multiple", "O quebra-cabeça tem mais de uma solução." },
                { "uniqueness.no-solution", "O quebra-cabeça não tem solução." }, { "uniqueness.invalid-input", "As pistas do quebra-cabeça são inválidas." },
                { "uniqueness.incomplete", "A verificação de soluções está incompleta." },
                { "control.hint", "Dica" }, { "control.clear", "Limpar" }, { "control.menu", "Menu" },
                { "control.new-game", "Novo jogo" }, { "control.back-to-game", "Voltar ao jogo" },
                { "settings.language", "Idioma" }, { "settings.device-language", "Idioma do dispositivo" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            }),
            new LocaleResource("es", new Dictionary<string, string>
            {
                { "line.row", "Fila" }, { "line.column", "Columna" },
                { "direction.up", "hacia arriba" }, { "direction.down", "hacia abajo" }, { "direction.left", "hacia la izquierda" }, { "direction.right", "hacia la derecha" },
                { "deduction.zero-line", "{line} {index} no necesita gatos. Las casillas restantes no pueden contener gatos." },
                { "deduction.line-satisfied", "{line} {index} ya tiene los gatos necesarios ({total}). Las casillas restantes no pueden contener gatos." },
                { "deduction.fill-remaining-line", "{line} {index} necesita un gato en cada casilla restante (pendientes: {count})." },
                { "deduction.arrow-singleton", "La flecha que apunta {direction} en la fila {row}, columna {column} solo tiene una casilla posible para un gato. Esa casilla debe contener un gato." },
                { "deduction.arrow-line-exclusion", "La flecha en la fila {row}, columna {column}, que apunta {direction}, exige que el último gato de {line} {index} esté en la dirección indicada. Las casillas fuera de ese segmento no pueden contener gatos." },
                { "solver.solved", "Resuelto mediante deducciones." }, { "solver.stalled", "No hay más deducciones disponibles." },
                { "solver.contradiction", "Estas pistas no pueden cumplirse todas a la vez." }, { "solver.invalid-input", "Las pistas del rompecabezas no son válidas." },
                { "solver.budget-exceeded", "Se alcanzó el límite de deducciones." },
                { "uniqueness.unique", "El rompecabezas tiene exactamente una solución." }, { "uniqueness.multiple", "El rompecabezas tiene más de una solución." },
                { "uniqueness.no-solution", "El rompecabezas no tiene solución." }, { "uniqueness.invalid-input", "Las pistas del rompecabezas no son válidas." },
                { "uniqueness.incomplete", "La comprobación de soluciones está incompleta." },
                { "control.hint", "Pista" }, { "control.clear", "Borrar" }, { "control.menu", "Menú" },
                { "control.new-game", "Nueva partida" }, { "control.back-to-game", "Volver al juego" },
                { "settings.language", "Idioma" }, { "settings.device-language", "Idioma del dispositivo" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            })
        });
    }
}
