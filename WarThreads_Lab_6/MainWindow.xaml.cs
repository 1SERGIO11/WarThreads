using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WarThreads_Lab_6
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTimer;
        private List<Image> enemies = new List<Image>();
        private List<Rectangle> bullets = new List<Rectangle>();
        private int maxBullets = 3;
        private int score = 0;
        private int missedEnemies = 0;
        private bool isGameRunning = false;
        private double enemySpeed = 1.5; // Скорость движения противников
        private double enemySpawnInterval = 1.0; // Интервал появления противников в секундах
        private DateTime lastEnemySpawnTime;

        private List<Image> powerUps = new List<Image>();
        private bool isPowerUpActive = false;
        private DateTime powerUpStartTime;
        private const double powerUpDuration = 10.0; // Длительность улучшалки в секундах

        private bool isMovingLeft = false;
        private bool isMovingRight = false;
        private const double playerSpeed = 10; // Увеличение скорости движения танка

        private const double maxBulletHeight = 700; // Максимальная высота для полета пули

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Task.Run(() => PlayerMovementLoop());
        }

        private void InitializeGame()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(30); // Обновляем каждые 30 миллисекунд для плавного движения
            gameTimer.Tick += GameLoop;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
            Keyboard.ClearFocus();
            this.Focus(); // Установить фокус на окно
        }

        private void StartGame()
        {
            score = 0;
            missedEnemies = 0;
            isGameRunning = true;
            ScoreTextBlock.Text = score.ToString();
            MissedTextBlock.Text = missedEnemies.ToString();
            enemies.Clear();
            bullets.Clear();
            powerUps.Clear();
            GameCanvas.Children.Clear();
            GameCanvas.Children.Add(Player);
            lastEnemySpawnTime = DateTime.Now;
            gameTimer.Start();

            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            ContinueButton.Visibility = Visibility.Collapsed;
            RestartButton.Visibility = Visibility.Collapsed;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopGame();
        }

        private void StopGame()
        {
            isGameRunning = false;
            gameTimer.Stop();

            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Collapsed;
            ContinueButton.Visibility = Visibility.Visible;
            RestartButton.Visibility = Visibility.Visible;
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            ContinueGame();
        }

        private void ContinueGame()
        {
            isGameRunning = true;
            gameTimer.Start();

            ContinueButton.Visibility = Visibility.Collapsed;
            RestartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!isGameRunning) return;

            if (isPowerUpActive && DateTime.Now.Subtract(powerUpStartTime).TotalSeconds >= powerUpDuration)
            {
                DeactivatePowerUp();
            }

            if (DateTime.Now.Subtract(lastEnemySpawnTime).TotalSeconds >= enemySpawnInterval)
            {
                CreateEnemy();
                lastEnemySpawnTime = DateTime.Now;
            }

            MoveEntities();
            CheckCollisions();
        }

        private void DeactivatePowerUp()
        {
            isPowerUpActive = false;
            maxBullets = 3; // Возвращаем ограничение на количество пуль
        }

        private void CreateEnemy()
        {
            Random rand = new Random();
            int enemyPosition = rand.Next(0, (int)GameCanvas.ActualWidth - 50);

            Dispatcher.Invoke(() =>
            {
                Image newEntity;
                if (rand.Next(0, 100) == 0) // 1/100 вероятность появления улучшалки
                {
                    newEntity = new Image
                    {
                        Width = 50,
                        Height = 50,
                        Source = new BitmapImage(new Uri("pack://application:,,,/images/Power.png"))
                    };
                    powerUps.Add(newEntity);
                }
                else
                {
                    newEntity = new Image
                    {
                        Width = 50,
                        Height = 50,
                        Source = new BitmapImage(new Uri("pack://application:,,,/images/enemy.png"))
                    };
                    enemies.Add(newEntity);
                }
                Canvas.SetLeft(newEntity, enemyPosition);
                Canvas.SetTop(newEntity, 0);
                GameCanvas.Children.Add(newEntity);
            });
        }

        private void MoveEntities()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (Image entity in enemies.Concat(powerUps).ToList())
                {
                    double top = Canvas.GetTop(entity);
                    Canvas.SetTop(entity, top + enemySpeed);

                    // Проверка, если враг пересек "Стоп-линию"
                    if (top >= GameCanvas.ActualHeight - 55)
                    {
                        if (enemies.Contains(entity))
                        {
                            enemies.Remove(entity);
                            missedEnemies++;
                            MissedTextBlock.Text = missedEnemies.ToString();
                            if (missedEnemies >= 30)
                            {
                                EndGame();
                            }
                        }
                        else
                        {
                            powerUps.Remove(entity);
                        }
                        GameCanvas.Children.Remove(entity);
                    }
                }
            });
        }

        private void CheckCollisions()
        {
            List<Rectangle> bulletsToRemove = new List<Rectangle>();
            List<Image> enemiesToRemove = new List<Image>();
            List<Image> powerUpsToRemove = new List<Image>();

            Dispatcher.Invoke(() =>
            {
                foreach (Rectangle bullet in bullets.ToList())
                {
                    double bulletLeft = Canvas.GetLeft(bullet);
                    double bulletTop = Canvas.GetTop(bullet);
                    double bulletRight = bulletLeft + bullet.Width;
                    double bulletBottom = bulletTop + bullet.Height;

                    if (bulletBottom > maxBulletHeight) // Ограничиваем полет пули
                    {
                        bulletsToRemove.Add(bullet);
                        continue;
                    }

                    foreach (Image enemy in enemies.ToList())
                    {
                        double enemyLeft = Canvas.GetLeft(enemy);
                        double enemyTop = Canvas.GetTop(enemy);
                        double enemyRight = enemyLeft + enemy.Width;
                        double enemyBottom = enemyTop + enemy.Height;

                        if (bulletRight >= enemyLeft && bulletLeft <= enemyRight &&
                            bulletBottom >= enemyTop && bulletTop <= enemyBottom)
                        {
                            bulletsToRemove.Add(bullet);
                            enemiesToRemove.Add(enemy);
                            score++;
                            ScoreTextBlock.Text = score.ToString();
                            break; // добавим break, чтобы избежать повторной обработки этого выстрела
                        }
                    }

                    foreach (Image powerUp in powerUps.ToList())
                    {
                        double powerUpLeft = Canvas.GetLeft(powerUp);
                        double powerUpTop = Canvas.GetTop(powerUp);
                        double powerUpRight = powerUpLeft + powerUp.Width;
                        double powerUpBottom = powerUpTop + powerUp.Height;

                        if (bulletRight >= powerUpLeft && bulletLeft <= powerUpRight &&
                            bulletBottom >= powerUpTop && bulletTop <= powerUpBottom)
                        {
                            bulletsToRemove.Add(bullet);
                            powerUpsToRemove.Add(powerUp);
                            ActivatePowerUp();
                            break; // добавим break, чтобы избежать повторной обработки этого выстрела
                        }
                    }
                }

                foreach (Rectangle bullet in bulletsToRemove)
                {
                    if (GameCanvas.Children.Contains(bullet))
                    {
                        GameCanvas.Children.Remove(bullet);
                    }
                    bullets.Remove(bullet);
                }

                foreach (Image enemy in enemiesToRemove)
                {
                    if (GameCanvas.Children.Contains(enemy))
                    {
                        enemies.Remove(enemy);
                        GameCanvas.Children.Remove(enemy);
                    }
                }

                foreach (Image powerUp in powerUpsToRemove)
                {
                    if (GameCanvas.Children.Contains(powerUp))
                    {
                        powerUps.Remove(powerUp);
                        GameCanvas.Children.Remove(powerUp);
                    }
                }
            });
        }

        private void ActivatePowerUp()
        {
            isPowerUpActive = true;
            powerUpStartTime = DateTime.Now;
            maxBullets = int.MaxValue; // Снимаем ограничение на количество пуль
        }

        private void EndGame()
        {
            Dispatcher.Invoke(() =>
            {
                isGameRunning = false;
                gameTimer.Stop();
                MessageBox.Show("Игра окончена! Ваш счет: " + score);
                StopButton.Visibility = Visibility.Collapsed;
                StartButton.Visibility = Visibility.Visible;
                ContinueButton.Visibility = Visibility.Collapsed;
                RestartButton.Visibility = Visibility.Collapsed;
            });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isGameRunning) return;

            if (e.Key == Key.Left)
            {
                isMovingLeft = true;
            }
            else if (e.Key == Key.Right)
            {
                isMovingRight = true;
            }
            else if (e.Key == Key.Space)
            {
                Task.Run(() => Shoot());
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (!isGameRunning) return;

            if (e.Key == Key.Left)
            {
                isMovingLeft = false;
            }
            else if (e.Key == Key.Right)
            {
                isMovingRight = false;
            }
        }

        private void PlayerMovementLoop()
        {
            while (true)
            {
                if (isMovingLeft)
                {
                    Dispatcher.Invoke(() =>
                    {
                        double left = Canvas.GetLeft(Player);
                        if (left > 0)
                        {
                            Canvas.SetLeft(Player, left - playerSpeed);
                        }
                    });
                }
                else if (isMovingRight)
                {
                    Dispatcher.Invoke(() =>
                    {
                        double left = Canvas.GetLeft(Player);
                        if (left < GameCanvas.ActualWidth - Player.Width)
                        {
                            Canvas.SetLeft(Player, left + playerSpeed);
                        }
                    });
                }

                Thread.Sleep(20); // Уменьшение интервала для более частых проверок
            }
        }

        private void Shoot()
        {
            if (bullets.Count >= maxBullets) return;

            Dispatcher.Invoke(() =>
            {
                Rectangle bullet = new Rectangle
                {
                    Width = 5,
                    Height = 20,
                    Fill = Brushes.Yellow
                };

                double left = Canvas.GetLeft(Player) + (Player.Width / 2) - (bullet.Width / 2);
                double top = Canvas.GetTop(Player) - bullet.Height;

                Canvas.SetLeft(bullet, left);
                Canvas.SetTop(bullet, top);

                GameCanvas.Children.Add(bullet);
                bullets.Add(bullet);

                Task.Run(() => MoveBullet(bullet));
            });
        }

        private async Task MoveBullet(Rectangle bullet)
        {
            while (true)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    double top = Canvas.GetTop(bullet);
                    if (top <= 0)
                    {
                        if (GameCanvas.Children.Contains(bullet))
                        {
                            GameCanvas.Children.Remove(bullet);
                        }
                        bullets.Remove(bullet);
                        return;
                    }

                    Canvas.SetTop(bullet, top - 10);
                });

                if (CheckBulletCollision(bullet))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (GameCanvas.Children.Contains(bullet))
                        {
                            GameCanvas.Children.Remove(bullet);
                        }
                        bullets.Remove(bullet);
                    });
                    return;
                }

                await Task.Delay(30);
            }
        }

        private bool CheckBulletCollision(Rectangle bullet)
        {
            bool collision = false;
            Dispatcher.Invoke(() =>
            {
                double bulletLeft = Canvas.GetLeft(bullet);
                double bulletTop = Canvas.GetTop(bullet);
                double bulletRight = bulletLeft + bullet.Width;
                double bulletBottom = bulletTop + bullet.Height;

                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    Image enemy = enemies[i];
                    double enemyLeft = Canvas.GetLeft(enemy);
                    double enemyTop = Canvas.GetTop(enemy);
                    double enemyRight = enemyLeft + enemy.Width;
                    double enemyBottom = enemyTop + enemy.Height;

                    if (bulletRight >= enemyLeft && bulletLeft <= enemyRight &&
                        bulletBottom >= enemyTop && bulletTop <= enemyBottom)
                    {
                        enemies.RemoveAt(i);
                        GameCanvas.Children.Remove(enemy);
                        score++;
                        ScoreTextBlock.Text = score.ToString();
                        collision = true;
                        break;
                    }
                }

                for (int i = powerUps.Count - 1; i >= 0; i--)
                {
                    Image powerUp = powerUps[i];
                    double powerUpLeft = Canvas.GetLeft(powerUp);
                    double powerUpTop = Canvas.GetTop(powerUp);
                    double powerUpRight = powerUpLeft + powerUp.Width;
                    double powerUpBottom = powerUpTop + powerUp.Height;

                    if (bulletRight >= powerUpLeft && bulletLeft <= powerUpRight &&
                        bulletBottom >= powerUpTop && bulletTop <= powerUpBottom)
                    {
                        powerUps.RemoveAt(i);
                        GameCanvas.Children.Remove(powerUp);
                        ActivatePowerUp();
                        collision = true;
                        break;
                    }
                }
            });

            return collision;
        }
    }
}
