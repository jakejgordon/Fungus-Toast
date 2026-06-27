using System.Collections.Generic;

namespace FungusToast.Core.Board
{
    public static partial class CampaignBoardStartingPositionCatalog
    {
        private static readonly Dictionary<(string PresetId, int PlayerCount), CampaignBoardStartingPositionMetadata> MetadataByPresetAndPlayerCount =
            new()
            {
                [("Campaign0", 2)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign0",
                    shapeKey: "51f263f81c95ee45ed1f73f3c146b6e760493c9e05be1186bb29f6e90970c46a",
                    boardWidth: 10,
                    boardHeight: 10,
                    playerCount: 2,
                    spriteName: "seed_cracker_550x550.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 7, y: 5, favorRank: 1, winPercentage: 76.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 2, y: 5, favorRank: 2, winPercentage: 66.000000),
                    }),

                [("Campaign1", 2)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign1",
                    shapeKey: "90c4270a787b86ec2c8caa224673d7d1117bdd688d842b726c7f92e094d0d05d",
                    boardWidth: 15,
                    boardHeight: 15,
                    playerCount: 2,
                    spriteName: "seed_cracker_550x550.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 11, y: 7, favorRank: 2, winPercentage: 94.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 3, y: 7, favorRank: 1, winPercentage: 94.000000),
                    }),

                [("Campaign10", 6)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign10",
                    shapeKey: "8dd7c94beb9680ac0b774fd68ecc14793a66d9bfa87e1ea8842f66437b89916a",
                    boardWidth: 115,
                    boardHeight: 115,
                    playerCount: 6,
                    spriteName: "hotdog_bun_900x900.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 98, y: 68, favorRank: 4, winPercentage: 5.882353),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 66, y: 79, favorRank: 2, winPercentage: 6.250000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 27, y: 80, favorRank: 3, winPercentage: 6.250000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 17, y: 47, favorRank: 5, winPercentage: 5.882353),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 48, y: 33, favorRank: 1, winPercentage: 11.764706),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 88, y: 34, favorRank: 6, winPercentage: 5.882353),
                    }),

                [("Campaign11", 7)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign11",
                    shapeKey: "07563e5adc75cddbaee7fcd5fb1e427b94ab5bdf63d36168c745d0901606a23b",
                    boardWidth: 120,
                    boardHeight: 120,
                    playerCount: 7,
                    spriteName: "hotdog_bun_900x900.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 104, y: 70, favorRank: 4, winPercentage: 13.333333),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 73, y: 82, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 40, y: 84, favorRank: 2, winPercentage: 14.285714),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 16, y: 70, favorRank: 1, winPercentage: 14.285714),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 25, y: 35, favorRank: 5, winPercentage: 7.142857),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 61, y: 35, favorRank: 3, winPercentage: 14.285714),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 96, y: 36, favorRank: 7, winPercentage: 0.000000),
                    }),

                [("Campaign12", 8)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign12",
                    shapeKey: "bc6933f092714f981f68adbb73716487327aa317c0e1104b1c7658f8ef241ffc",
                    boardWidth: 130,
                    boardHeight: 130,
                    playerCount: 8,
                    spriteName: "pita_900x900.png",
                    shapeSource: "ellipse-shape",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 115, y: 86, favorRank: 5, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 86, y: 115, favorRank: 1, winPercentage: 8.333333),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 44, y: 115, favorRank: 4, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 15, y: 86, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 15, y: 44, favorRank: 8, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 44, y: 15, favorRank: 2, winPercentage: 7.692308),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 86, y: 15, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 7, x: 115, y: 44, favorRank: 3, winPercentage: 7.692308),
                    }),

                [("Campaign13", 8)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign13",
                    shapeKey: "b93b5e743e9ab8c4c289da0dd946ee6644b7dfc629ac42cd287361b07a6c38f9",
                    boardWidth: 140,
                    boardHeight: 140,
                    playerCount: 8,
                    spriteName: "pita_900x900.png",
                    shapeSource: "ellipse-shape",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 124, y: 93, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 93, y: 124, favorRank: 8, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 47, y: 124, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 16, y: 93, favorRank: 4, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 16, y: 47, favorRank: 1, winPercentage: 8.333333),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 47, y: 16, favorRank: 3, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 93, y: 16, favorRank: 2, winPercentage: 7.692308),
                        new CampaignBoardStartingPositionEntry(slotIndex: 7, x: 124, y: 47, favorRank: 5, winPercentage: 0.000000),
                    }),

                [("Campaign14", 8)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign14",
                    shapeKey: "4909979cef9b452bc97c45f9246feb4bfc6878a85b8df537e5886926138335bc",
                    boardWidth: 150,
                    boardHeight: 150,
                    playerCount: 8,
                    spriteName: "pita_900x900.png",
                    shapeSource: "ellipse-shape",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 133, y: 99, favorRank: 1, winPercentage: 7.692308),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 99, y: 133, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 51, y: 133, favorRank: 3, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 17, y: 99, favorRank: 8, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 17, y: 51, favorRank: 4, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 51, y: 17, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 99, y: 17, favorRank: 5, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 7, x: 133, y: 51, favorRank: 2, winPercentage: 7.692308),
                    }),

                [("Campaign15", 8)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign15",
                    shapeKey: "13419f25f7b79da8aca03530a8017ce063f0e076bbad7ae7f7e50fa1dfd8f3b6",
                    boardWidth: 160,
                    boardHeight: 160,
                    playerCount: 8,
                    spriteName: "pita_900x900.png",
                    shapeSource: "ellipse-shape",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 142, y: 106, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 106, y: 142, favorRank: 5, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 54, y: 142, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 18, y: 106, favorRank: 8, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 18, y: 54, favorRank: 2, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 54, y: 18, favorRank: 4, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 106, y: 18, favorRank: 1, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 7, x: 142, y: 54, favorRank: 3, winPercentage: 0.000000),
                    }),

                [("Campaign2", 3)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign2",
                    shapeKey: "580c7dba081147d19fd8430f6a8e065243b717ef9dae220717bca5e98e607e1b",
                    boardWidth: 20,
                    boardHeight: 20,
                    playerCount: 3,
                    spriteName: "seed_cracker_550x550.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 17, y: 10, favorRank: 1, winPercentage: 88.235294),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 6, y: 16, favorRank: 3, winPercentage: 66.666667),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 6, y: 3, favorRank: 2, winPercentage: 72.727273),
                    }),

                [("Campaign3", 4)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign3",
                    shapeKey: "c9692b1270b108724ddc6ab78293be80172e24fe435525a2d8e3376ddcdcfb87",
                    boardWidth: 30,
                    boardHeight: 30,
                    playerCount: 4,
                    spriteName: "cracker_final_600x600.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 23, y: 23, favorRank: 1, winPercentage: 80.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 6, y: 23, favorRank: 3, winPercentage: 68.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 6, y: 6, favorRank: 2, winPercentage: 76.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 23, y: 6, favorRank: 4, winPercentage: 52.000000),
                    }),

                [("Campaign4", 5)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign4",
                    shapeKey: "c9910ceeb76b07255aa4e1155f0a43b00baf579ba43897aaff558d174f88240c",
                    boardWidth: 40,
                    boardHeight: 40,
                    playerCount: 5,
                    spriteName: "cracker_final_600x600.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 28, y: 26, favorRank: 1, winPercentage: 55.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 16, y: 29, favorRank: 3, winPercentage: 45.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 9, y: 20, favorRank: 2, winPercentage: 45.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 16, y: 10, favorRank: 4, winPercentage: 35.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 28, y: 14, favorRank: 5, winPercentage: 35.000000),
                    }),

                [("Campaign5", 6)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign5",
                    shapeKey: "38468d8910d24e7d392255aa7b971d59ba876f43d6e978b6167fabf30c30ac99",
                    boardWidth: 50,
                    boardHeight: 50,
                    playerCount: 6,
                    spriteName: "cheese_800x800.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 42, y: 29, favorRank: 6, winPercentage: 17.647059),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 28, y: 39, favorRank: 2, winPercentage: 43.750000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 11, y: 38, favorRank: 3, winPercentage: 43.750000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 7, y: 20, favorRank: 4, winPercentage: 29.411765),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 21, y: 10, favorRank: 1, winPercentage: 47.058824),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 38, y: 11, favorRank: 5, winPercentage: 29.411765),
                    }),

                [("Campaign6", 5)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign6",
                    shapeKey: "de3d21345c4d929f60c9d3d9d7fce02203617befe4383433744ecf5f8d63d5df",
                    boardWidth: 75,
                    boardHeight: 75,
                    playerCount: 5,
                    spriteName: "cheese_800x800.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 53, y: 48, favorRank: 4, winPercentage: 20.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 31, y: 56, favorRank: 1, winPercentage: 35.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 18, y: 37, favorRank: 5, winPercentage: 15.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 31, y: 19, favorRank: 2, winPercentage: 30.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 53, y: 26, favorRank: 3, winPercentage: 25.000000),
                    }),

                [("Campaign7", 7)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign7",
                    shapeKey: "a5e2cadbccdf1b216e74dd47194791796fdcab166417310cb093804342dccf8a",
                    boardWidth: 90,
                    boardHeight: 90,
                    playerCount: 7,
                    spriteName: "kaiser_bun_1086x1448.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 78, y: 53, favorRank: 5, winPercentage: 6.666667),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 59, y: 76, favorRank: 4, winPercentage: 7.142857),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 30, y: 76, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 12, y: 53, favorRank: 1, winPercentage: 21.428571),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 18, y: 24, favorRank: 2, winPercentage: 21.428571),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 45, y: 11, favorRank: 3, winPercentage: 14.285714),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 72, y: 24, favorRank: 6, winPercentage: 0.000000),
                    }),

                [("Campaign8", 7)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign8",
                    shapeKey: "e64068a6a171e3a7f0552aed65b815c93df56e9221554b4f18d0289d69199523",
                    boardWidth: 100,
                    boardHeight: 100,
                    playerCount: 7,
                    spriteName: "white_bread_1024x1024.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 87, y: 59, favorRank: 5, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 66, y: 84, favorRank: 3, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 34, y: 84, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 13, y: 59, favorRank: 1, winPercentage: 7.142857),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 20, y: 26, favorRank: 4, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 50, y: 12, favorRank: 2, winPercentage: 7.142857),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 80, y: 26, favorRank: 6, winPercentage: 0.000000),
                    }),

                [("Campaign9", 7)] = new CampaignBoardStartingPositionMetadata(
                    presetId: "Campaign9",
                    shapeKey: "f9837cccea471e8a98be5d184f739364ef2b0eca2134dc14812faac9d34c8d2e",
                    boardWidth: 110,
                    boardHeight: 110,
                    playerCount: 7,
                    spriteName: "white_bread_1024x1024.png",
                    shapeSource: "baked-mask",
                    entries: new CampaignBoardStartingPositionEntry[]
                    {
                        new CampaignBoardStartingPositionEntry(slotIndex: 0, x: 95, y: 64, favorRank: 4, winPercentage: 13.333333),
                        new CampaignBoardStartingPositionEntry(slotIndex: 1, x: 73, y: 93, favorRank: 3, winPercentage: 28.571429),
                        new CampaignBoardStartingPositionEntry(slotIndex: 2, x: 37, y: 93, favorRank: 7, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 3, x: 14, y: 64, favorRank: 1, winPercentage: 35.714286),
                        new CampaignBoardStartingPositionEntry(slotIndex: 4, x: 22, y: 29, favorRank: 2, winPercentage: 28.571429),
                        new CampaignBoardStartingPositionEntry(slotIndex: 5, x: 55, y: 13, favorRank: 6, winPercentage: 0.000000),
                        new CampaignBoardStartingPositionEntry(slotIndex: 6, x: 88, y: 29, favorRank: 5, winPercentage: 6.666667),
                    }),

            };
    }
}
