CREATE TABLE [dbo].[classnoun] (
    [word_id]             INT          NOT NULL,
    [word]                VARCHAR (50) NOT NULL,
    [plural]              VARCHAR (50) NOT NULL,
    [plural_spoken_rec]   VARCHAR (50) NULL,
    [plural_spoken_synth] VARCHAR (50) NULL,
    CONSTRAINT [PK_classnoun] PRIMARY KEY CLUSTERED ([word_id] ASC),
    CONSTRAINT [FK_ud_word_classnoun] FOREIGN KEY ([word_id]) REFERENCES [dbo].[ud_word] ([id]) ON DELETE CASCADE
);

