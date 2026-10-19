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
                { "direction.upleft", "diagonally up and left" }, { "direction.upright", "diagonally up and right" },
                { "direction.downleft", "diagonally down and left" }, { "direction.downright", "diagonally down and right" },
                { "help.diagonal-arrows", "A diagonal arrow needs at least one paw along its exact diagonal to the board edge. For example, a down-right arrow points one row down and one column right at each step, continuing through other arrows." },
                { "deduction.zero-line", "{line} {index} needs no paws. Its remaining cells cannot contain paws." },
                { "deduction.line-satisfied", "{line} {index} already has its required paws ({total}). Its remaining cells cannot contain paws." },
                { "deduction.fill-remaining-line", "{line} {index} needs a paw in each remaining cell ({count} remaining)." },
                { "deduction.arrow-singleton", "The arrow pointing {direction} at row {row}, column {column} has only one possible paw cell left. That cell must contain a paw." },
                { "deduction.arrow-line-exclusion", "The arrow at row {row}, column {column}, pointing {direction}, requires the last paw in {line} {index} to lie in its direction. Cells outside that segment cannot contain paws." },
                { "solver.solved", "Solved using deductions." }, { "solver.stalled", "No further deduction is available." },
                { "solver.contradiction", "These clues cannot all be satisfied." }, { "solver.invalid-input", "The puzzle clues are invalid." },
                { "solver.budget-exceeded", "The deduction limit was reached." },
                { "uniqueness.unique", "The puzzle has exactly one solution." }, { "uniqueness.multiple", "The puzzle has more than one solution." },
                { "uniqueness.no-solution", "The puzzle has no solution." }, { "uniqueness.invalid-input", "The puzzle clues are invalid." },
                { "uniqueness.incomplete", "The solution check is incomplete." },
                { "generation.success", "Your puzzle is ready." },
                { "generation.invalid-profile", "Unable to create a puzzle with these settings." },
                { "generation.unsupported-profile", "This puzzle difficulty is unavailable." },
                { "generation.exhausted", "Unable to create a puzzle. Please start a new game." },
                { "generation.cancelled", "Puzzle creation was cancelled." },
                { "control.hint", "Hint" }, { "control.clear", "Clear" }, { "control.menu", "Menu" },
                { "control.new-game", "New Game" }, { "control.back-to-game", "Back to the Game" },
                { "settings.language", "Language" }, { "settings.device-language", "Device language" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            }),
            new LocaleResource("pt-BR", new Dictionary<string, string>
            {
                { "line.row", "Linha" }, { "line.column", "Coluna" },
                { "direction.up", "para cima" }, { "direction.down", "para baixo" }, { "direction.left", "para a esquerda" }, { "direction.right", "para a direita" },
                { "direction.upleft", "na diagonal para cima e para a esquerda" }, { "direction.upright", "na diagonal para cima e para a direita" },
                { "direction.downleft", "na diagonal para baixo e para a esquerda" }, { "direction.downright", "na diagonal para baixo e para a direita" },
                { "help.diagonal-arrows", "Uma seta diagonal precisa de pelo menos uma pata em sua diagonal exata até a borda do tabuleiro. Por exemplo, uma seta para baixo e para a direita aponta uma linha abaixo e uma coluna à direita a cada passo, passando por outras setas." },
                { "deduction.zero-line", "{line} {index} não precisa de patas. As células restantes não podem conter patas." },
                { "deduction.line-satisfied", "{line} {index} já tem as patas necessárias ({total}). As células restantes não podem conter patas." },
                { "deduction.fill-remaining-line", "{line} {index} precisa de uma pata em cada célula restante (restantes: {count})." },
                { "deduction.arrow-singleton", "A seta apontando {direction} na linha {row}, coluna {column} tem apenas uma célula possível para uma pata. Essa célula deve conter uma pata." },
                { "deduction.arrow-line-exclusion", "A seta na linha {row}, coluna {column}, apontando {direction}, exige que a última pata da {line} {index} esteja na direção indicada. As células fora desse segmento não podem conter patas." },
                { "solver.solved", "Resolvido usando deduções." }, { "solver.stalled", "Nenhuma outra dedução está disponível." },
                { "solver.contradiction", "Estas pistas não podem ser todas satisfeitas." }, { "solver.invalid-input", "As pistas do quebra-cabeça são inválidas." },
                { "solver.budget-exceeded", "O limite de deduções foi atingido." },
                { "uniqueness.unique", "O quebra-cabeça tem exatamente uma solução." }, { "uniqueness.multiple", "O quebra-cabeça tem mais de uma solução." },
                { "uniqueness.no-solution", "O quebra-cabeça não tem solução." }, { "uniqueness.invalid-input", "As pistas do quebra-cabeça são inválidas." },
                { "uniqueness.incomplete", "A verificação de soluções está incompleta." },
                { "generation.success", "Seu quebra-cabeça está pronto." },
                { "generation.invalid-profile", "Não é possível criar um quebra-cabeça com estas configurações." },
                { "generation.unsupported-profile", "Esta dificuldade de quebra-cabeça não está disponível." },
                { "generation.exhausted", "Não foi possível criar um quebra-cabeça. Inicie um novo jogo." },
                { "generation.cancelled", "A criação do quebra-cabeça foi cancelada." },
                { "control.hint", "Dica" }, { "control.clear", "Limpar" }, { "control.menu", "Menu" },
                { "control.new-game", "Novo jogo" }, { "control.back-to-game", "Voltar ao jogo" },
                { "settings.language", "Idioma" }, { "settings.device-language", "Idioma do dispositivo" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            }),
            new LocaleResource("es", new Dictionary<string, string>
            {
                { "line.row", "Fila" }, { "line.column", "Columna" },
                { "direction.up", "hacia arriba" }, { "direction.down", "hacia abajo" }, { "direction.left", "hacia la izquierda" }, { "direction.right", "hacia la derecha" },
                { "direction.upleft", "en diagonal hacia arriba y a la izquierda" }, { "direction.upright", "en diagonal hacia arriba y a la derecha" },
                { "direction.downleft", "en diagonal hacia abajo y a la izquierda" }, { "direction.downright", "en diagonal hacia abajo y a la derecha" },
                { "help.diagonal-arrows", "Una flecha diagonal necesita al menos una pata en su diagonal exacta hasta el borde del tablero. Por ejemplo, una flecha hacia abajo y a la derecha apunta una fila abajo y una columna a la derecha en cada paso, pasando por otras flechas." },
                { "deduction.zero-line", "{line} {index} no necesita patas. Las casillas restantes no pueden contener patas." },
                { "deduction.line-satisfied", "{line} {index} ya tiene las patas necesarias ({total}). Las casillas restantes no pueden contener patas." },
                { "deduction.fill-remaining-line", "{line} {index} necesita una pata en cada casilla restante (pendientes: {count})." },
                { "deduction.arrow-singleton", "La flecha que apunta {direction} en la fila {row}, columna {column} solo tiene una casilla posible para una pata. Esa casilla debe contener una pata." },
                { "deduction.arrow-line-exclusion", "La flecha en la fila {row}, columna {column}, que apunta {direction}, exige que la última pata de {line} {index} esté en la dirección indicada. Las casillas fuera de ese segmento no pueden contener patas." },
                { "solver.solved", "Resuelto mediante deducciones." }, { "solver.stalled", "No hay más deducciones disponibles." },
                { "solver.contradiction", "Estas pistas no pueden cumplirse todas a la vez." }, { "solver.invalid-input", "Las pistas del rompecabezas no son válidas." },
                { "solver.budget-exceeded", "Se alcanzó el límite de deducciones." },
                { "uniqueness.unique", "El rompecabezas tiene exactamente una solución." }, { "uniqueness.multiple", "El rompecabezas tiene más de una solución." },
                { "uniqueness.no-solution", "El rompecabezas no tiene solución." }, { "uniqueness.invalid-input", "Las pistas del rompecabezas no son válidas." },
                { "uniqueness.incomplete", "La comprobación de soluciones está incompleta." },
                { "generation.success", "Tu rompecabezas está listo." },
                { "generation.invalid-profile", "No es posible crear un rompecabezas con esta configuración." },
                { "generation.unsupported-profile", "Esta dificultad de rompecabezas no está disponible." },
                { "generation.exhausted", "No se pudo crear un rompecabezas. Inicia una nueva partida." },
                { "generation.cancelled", "Se canceló la creación del rompecabezas." },
                { "control.hint", "Pista" }, { "control.clear", "Borrar" }, { "control.menu", "Menú" },
                { "control.new-game", "Nueva partida" }, { "control.back-to-game", "Volver al juego" },
                { "settings.language", "Idioma" }, { "settings.device-language", "Idioma del dispositivo" },
                { "language.en", "English" }, { "language.pt-BR", "Português (Brasil)" }, { "language.es", "Español" }
            })
        });
    }
}
