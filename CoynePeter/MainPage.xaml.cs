using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CoynePeter
{
    #region the player class
    class Player
    {
        public Frame piece { get; set; }
        public int position { get; set; }
        public string name { get; set; }
        public Player(string playerName, string avatarURI)
        {
            position = 0;
            name = playerName;
            piece = new Frame // need a Frame to round the avatars
            {
                Content = new Image { Source = ImageSource.FromFile(avatarURI), Aspect = Aspect.AspectFill },
                CornerRadius = 50,
                Padding = 0,
                IsClippedToBounds = true
            };
        }
    }
    #endregion

    #region the square class
    class Square
    {
        public int row { get; set; }
        public int col { get; set; }
        public int occupied { get; set; }
        public int redirect { get; set; }
        public Label sqLabel { get; set; }
        public Square(int sqX, int sqY)
        {
            row = sqX;
            col = sqY;
            occupied = 0;
            redirect = 0;
        }
    }
    #endregion

    public partial class MainPage : ContentPage
    {
        const int ROWS = 10;
        const int COLS = 10;
        const int NUMSQUARES = (ROWS * COLS) + 1;
        const int SQUARESIZE = 40;
        const int NUMPLAYERS = 3;

        int turn;

        Player[] players = new Player[NUMPLAYERS];
        Square[] squares = new Square[NUMSQUARES];
        Player winner;

        Random rnd = new Random();

        #region add the board
        Image imgBoard = new Image
        {
            Source = ImageSource.FromFile("board.png"),
            Aspect = Aspect.Fill
        };
        #endregion

        public MainPage()
        {
            InitializeComponent();
            InitialiseBoard();
        }

        private void InitialiseBoard()
        {
            turn = 0;
            int i, j;
            int squareID = NUMSQUARES - 1;

            #region Create row and column definitions
            for (i = 0; i < ROWS; i++)
            {
                GrdBoard.RowDefinitions.Add(new RowDefinition());
            }
            for (j = 0; j < COLS; j++)
            {
                GrdBoard.ColumnDefinitions.Add(new ColumnDefinition());
            }
            #endregion

            #region set board background
            Grid.SetRowSpan(imgBoard, ROWS);
            Grid.SetColumnSpan(imgBoard, COLS);
            GrdBoard.Children.Add(imgBoard);
            #endregion

            #region add squares
            for (i = 0; i < ROWS; i++)
            {
                if (i % 2 == 0) // every second row is reversed
                {
                    for (j = 0; j < COLS; j++)
                    {
                        squares[squareID] = new Square(i, j);
                        MakeLabel(squareID);
                        squareID--;
                    }
                }
                else
                {
                    for (j = COLS - 1; j >= 0; j--)
                    {
                        squares[squareID] = new Square(i, j);
                        MakeLabel(squareID);
                        squareID--;
                    }
                }
            }
            #endregion

            #region add snakes and ladders definitions
            squares[3].redirect = 22;
            squares[7].redirect = 5;
            squares[8].redirect = 27;
            squares[17].redirect = 45;
            squares[19].redirect = 2;
            squares[30].redirect = 52;
            squares[32].redirect = 12;
            squares[34].redirect = 73;
            squares[37].redirect = 25;
            squares[38].redirect = 61;
            squares[44].redirect = 74;
            squares[46].redirect = 15;
            squares[55].redirect = 85;
            squares[57].redirect = 40;
            squares[70].redirect = 48;
            squares[71].redirect = 92;
            squares[78].redirect = 97;
            squares[80].redirect = 41;
            squares[88].redirect = 35;
            squares[91].redirect = 72;
            squares[95].redirect = 75;
            squares[98].redirect = 63;
            #endregion

            #region set up players array
            players[0] = new Player("Damien", "damien.png");
            players[1] = new Player("WALL-E", "walle.png");
            players[2] = new Player("Eve", "eve.png");
            #endregion

            #region add players to lobby
            for (int player = 0; player < NUMPLAYERS; player++)
            {
                Frame piece = players[player].piece;
                piece.HeightRequest = 100;
                piece.WidthRequest = 100;
                piece.Margin = 4;
                SLLobby.Children.Add(piece);
            }
            #endregion

            #region prompt user to move
            LblFeedback.Text = players[0].name + "'s turn";
            #endregion
        }

        async void ButtonRollClicked(System.Object sender, System.EventArgs e)
        {
            Player currentPlayer = players[turn % NUMPLAYERS];
            Frame piece = currentPlayer.piece;
            bool finalMove = false;

            SLPage.Children.Remove(SLButtons); // remove the action buttons to prevent mid-move interference

            int roll = rnd.Next(1, 7); // roll the dice
            LblFeedback.Text = "Rolled a " + roll;

            #region if first move
            if (piece.Parent == SLLobby)
            {
                await piece.ScaleTo(0, 60, Easing.CubicIn); // shrink avatar from lobby
                SLLobby.Children.Remove(piece);
                await OrganiseSquare(1, false); // prepare square 1 for occupation
                currentPlayer.position = 1;
                squares[1].occupied++;
                piece.SetValue(Grid.RowProperty, squares[1].row);
                piece.SetValue(Grid.ColumnProperty, squares[1].col);
                GrdBoard.Children.Add(piece);
                await piece.ScaleTo(1, 40, Easing.CubicIn);
                await OrganiseSquare(1, false);
                
                roll--;
            }
            #endregion

            #region move square by square
            for (int i = 0; i < roll; i++) // move square by square
            {
                if (i == roll - 1)
                {
                    finalMove = true;
                }
                await MovePiece(currentPlayer, currentPlayer.position, currentPlayer.position + 1, finalMove);
            }
            #endregion

            #region detect win or continue game
            if (winner != null)
            {
                LblFeedback.Text = winner.name + " wins!";
            }
            else
            {
                turn++;
                LblFeedback.Text = players[turn % NUMPLAYERS].name + "'s turn";
            }
            #endregion

            SLPage.Children.Add(SLButtons); // restore the action buttons
        }

        private async Task OrganiseSquare(int sq, bool pave) // pave arranges avatars with space for an entry
        {
            #region make an array of all the players occupying the square
            int countedPlayers = 0;
            Frame[] piecesInSquare = new Frame[squares[sq].occupied];

            for (int player = 0; player < NUMPLAYERS; player++)
            {
                if (players[player].position == sq)
                {
                    piecesInSquare[countedPlayers] = players[player].piece;
                    countedPlayers++;
                }
            }
            #endregion

            #region arrangement patterns depending on whether we need to leave space for the player
            if (!pave) // check which pattern to arrange for
            {
                switch (countedPlayers)
                {
                    case 1:
                        await piecesInSquare[0].ScaleTo(1, 40, Easing.CubicIn);
                        await piecesInSquare[0].TranslateTo(0, 0, 40);
                        break;
                    case 2:
                        await piecesInSquare[0].ScaleTo(0.5, 20, Easing.CubicIn);
                        await piecesInSquare[0].TranslateTo(0, -10, 20);
                        await piecesInSquare[1].ScaleTo(0.5, 20, Easing.CubicIn);
                        await piecesInSquare[1].TranslateTo(0, 10, 20);
                        break;
                }
            }
            else
            {
                switch (countedPlayers)
                {
                    case 1:
                        await piecesInSquare[0].ScaleTo(0.5, 40, Easing.CubicIn);
                        await piecesInSquare[0].TranslateTo(0, -10, 40);
                        break;
                    case 2:
                        await piecesInSquare[0].ScaleTo(0.5, 20, Easing.CubicIn);
                        await piecesInSquare[0].TranslateTo(-10, -10, 20);
                        await piecesInSquare[1].ScaleTo(0.5, 20, Easing.CubicIn);
                        await piecesInSquare[1].TranslateTo(10, -10, 20);
                        break;
                }
            }
            #endregion
        }

        private async Task MovePiece(Player currentPlayer, int prev, int next, bool final)
        {
            Frame piece = currentPlayer.piece;

            #region calculate translation values based on grid
            int x1 = squares[prev].col;
            int y1 = squares[prev].row;
            int x2 = squares[next].col;
            int y2 = squares[next].row;
            int xTranslate = (x2 - x1) * SQUARESIZE;
            int yTranslate = (y2 - y1) * SQUARESIZE;
            #endregion

            #region scale/translate to bottom of square if it's occupied
            double scale = 1;
            int translateReset = 0;

            if (squares[next].occupied > 0)
            {
                scale = 0.5;
                yTranslate += 10;
                await OrganiseSquare(next, true);
                translateReset = 10;
            }
            #endregion

            #region update the model
            squares[next].occupied++;
            squares[prev].occupied--;
            currentPlayer.position = next;
            #endregion

            #region do actual scale and translation
            await piece.ScaleTo(scale, 80, Easing.CubicIn);
            await piece.TranslateTo(xTranslate, yTranslate, 80);
            if (squares[prev].occupied > 0)
            {
                await OrganiseSquare(prev, false);
            }
            #endregion

            #region place pieces into new grid slot and reset translation
            piece.SetValue(Grid.RowProperty, squares[next].row);
            piece.SetValue(Grid.ColumnProperty, squares[next].col);

            piece.TranslationY = translateReset;
            piece.TranslationX = 0;
            #endregion

            #region if we land on a snake/ladder, trigger the redirect
            if (final && squares[currentPlayer.position].redirect != 0)
            {
                await currentPlayer.piece.FadeTo(0, 200, Easing.CubicIn);
                await currentPlayer.piece.FadeTo(1, 200, Easing.CubicOut);
                await MovePiece(currentPlayer, currentPlayer.position, squares[currentPlayer.position].redirect, false);
            }
            #endregion

            if (next >= NUMSQUARES - 1)
            {
                winner = currentPlayer;
            }
        }

        void MakeLabel(int squareID)
        {
            int row = squares[squareID].row;
            int col = squares[squareID].col;
            Label LblSQ = new Label();
            LblSQ.Text = Convert.ToString(squareID);
            LblSQ.TextColor = Color.FromHex("00AAFF");
            LblSQ.Opacity = 0.5;
            LblSQ.FontSize = 10;
            LblSQ.Padding = new Thickness(4, 2, 0, 0);
            LblSQ.SetValue(Grid.RowProperty, row);
            LblSQ.SetValue(Grid.ColumnProperty, col);
            GrdBoard.Children.Add(LblSQ);
        }

        void ButtonResetClicked(System.Object sender, System.EventArgs e)
        {
            for (int i = 1; i < NUMSQUARES; i++)
            {
                squares[i].occupied = 0;
            }
            for (int player = 0; player < NUMPLAYERS; player++)
            {
                Frame piece = players[player].piece;
                GrdBoard.Children.Remove(piece);
                SLLobby.Children.Remove(piece);
                piece.HeightRequest = 100;
                piece.WidthRequest = 100;
                piece.Margin = 4;
                piece.TranslationX = 0;
                piece.TranslationY = 0;
                piece.Scale = 1;
                SLLobby.Children.Add(piece);
            }

            turn = 0;
            LblFeedback.Text = players[0].name + "'s turn";
        }
    }
}