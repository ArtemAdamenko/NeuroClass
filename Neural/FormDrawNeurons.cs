﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using ExcelLibrary.SpreadSheet;


namespace Neural
{
    public partial class FormDrawNeurons : Form
    {
        //Neural Net options
        private double[,] data = null;
        private double[][][][] tempWeights;
        int rowCountData = 0;
        int colCountData = 0;
        private int[] classes;
        List<int> classesList = new List<int>();
        private double validateError = 0.0;
        private int[] samplesPerClass;
        private String selectedType = "";
        private double testQuality = 0.0;
        private double offWeightsSumInput = 0.0;
        private double offWeightsSumAbsoluteInput = 0.0;
        private double offWeightsSumOutput = 0.0;
        private double offWeightsSumAbsoluteOutput = 0.0;

        //draw options
        private SolidBrush _myBrush = new SolidBrush(Color.DarkSeaGreen);
        private SolidBrush _offBrush = new SolidBrush(Color.Gray);
        private Pen _myPen = new Pen(Color.Black);

        //App options
        Network network = null;
        Thread Worker;
        List<string> connectedNeurons;

        string fileXlsNeurons;
        Workbook workbook;
        Worksheet worksheet;
        string fileXlsWeightsRel;
        Workbook workbook2;
        Worksheet worksheet2;
        string fileXlsWeightsAbs;
        Workbook workbook3;
        Worksheet worksheet3;

        public FormDrawNeurons()
        {
            InitializeComponent();
        }

        public void draw()
        {
            Bitmap bmp;
            Graphics formGraphics;

            // Create a bitmap the size of the form.
            //first layer does not contains in network.Layers, because +1
            
            int maxNeurons = 0;
            for (int layer = 0; layer < network.Layers.Length; layer++)
            {
                if (maxNeurons < network.Layers[layer].Neurons.Length)
                {
                    maxNeurons = network.Layers[layer].Neurons.Length;
                }
            }
            bmp = new Bitmap(200 * (network.Layers.Length+1), 50 * maxNeurons);
            formGraphics = Graphics.FromImage(bmp);
            int x = 0;

            //draw input layer
            int cntInput = network.InputsCount;
            _myPen.Width = 1;
            System.Drawing.Point[] fisrtPoints = new System.Drawing.Point[cntInput];
            //default color
            _myPen.Color = Color.Black;

            for (int k = 0; k < cntInput; k++)
            {
                Rectangle ellipse = new Rectangle(x, 50 * k, 50, 50);
                formGraphics.FillEllipse(this._myBrush, ellipse);
                formGraphics.DrawEllipse(this._myPen, ellipse);
                //draw string in ellipse
                formGraphics.DrawString("Input(" + k.ToString() + ")",
                                    new Font("Arial", 9, FontStyle.Regular),
                                    new SolidBrush(Color.Black),
                                    new System.Drawing.Point(x + 3, 50 * k + 15));
                fisrtPoints[k].X = x + 50;
                fisrtPoints[k].Y = 50 * k + (50 / 2);
            }


            System.Drawing.Point[] tempRightPoints = new System.Drawing.Point[1];
            //draw other layers
            for (int i = 0; i < network.Layers.Length; i++)
            {
                x = 200 * (i + 1);

                System.Drawing.Point[] hiddenLeftPoints = new System.Drawing.Point[network.Layers[i].Neurons.Length];
                System.Drawing.Point[] hiddenRightPoints = new System.Drawing.Point[network.Layers[i].Neurons.Length];

                for (int j = 0; j < network.Layers[i].Neurons.Length; j++)
                {
                    _myPen.Color = Color.Black;
                    Rectangle ellipse = new Rectangle(x, 50 * j, 50, 50);
                    formGraphics.FillEllipse(this._myBrush, ellipse);
                    formGraphics.DrawEllipse(this._myPen, ellipse);
                    //draw string in ellipse
                    formGraphics.DrawString("L(" + i.ToString() + ")N(" + j.ToString() + ")",
                                    new Font("Arial", 9, FontStyle.Regular),
                                    new SolidBrush(Color.Black),
                                    new System.Drawing.Point(x, 50 * j + 15));

                    hiddenLeftPoints[j].X = x;
                    hiddenLeftPoints[j].Y = 50 * j + (50 / 2);

                    hiddenRightPoints[j].X = x + 50;
                    hiddenRightPoints[j].Y = 50 * j + (50 / 2);

                    //if this first hidden layer
                    if (i == 0)
                    {
                        //all neurons n-1 layer
                        for (int b = 0; b < fisrtPoints.Length; b++)
                        {
                            //if weight current neuron == 0.0
                            if (network.Layers[i].Neurons[j].Weights[b] == 0.0)
                                _myPen.Color = Color.Red;
                            else
                                _myPen.Color = Color.Black;
                            formGraphics.DrawLine(_myPen, hiddenLeftPoints[j], fisrtPoints[b]);
                        }
                    }
                    else if (i != 0)
                    {
                        for (int c = 0; c < network.Layers[i - 1].Neurons.Length; c++)
                        {
                            //if weight current neuron == 0.0
                            if (network.Layers[i].Neurons[j].Weights[c] == 0.0)
                                _myPen.Color = Color.Red;
                            else
                                _myPen.Color = Color.Black;
                            formGraphics.DrawLine(_myPen, hiddenLeftPoints[j], tempRightPoints[c]);
                        }
                    }
                }
                //temp mass of right points of current layer for next cycle
                tempRightPoints = new System.Drawing.Point[network.Layers[i].Neurons.Length];
                tempRightPoints = hiddenRightPoints;
            }

            pictureBox1.Image = bmp;
        }



        /*
         * Заполняет таблицу значениями 
         * слоев нейронов и весов соответственно
         */
        private void setNeuronsDataGrid() 
        {
            // remove all current records
            this.dataGridView1.Rows.Clear();
            
            for (int i = 0; i < network.Layers.Length; i++)
            {
                for (int j = 0; j < network.Layers[i].Neurons.Length; j++)
                {
                    for (int k = 0; k < network.Layers[i].Neurons[j].Weights.Length; k++ )

                        this.dataGridView1.Rows.Add(i.ToString(), j.ToString(), k.ToString(), network.Layers[i].Neurons[j].Weights[k].ToString(), "F");
                }
            }

        }

        //Запуск сети для проверки
        private void CheckNeurons()
        {
            for (int i = 0; i < this.dataGridView1.RowCount; i++)
            {
                int layer = Int32.Parse(dataGridView1.Rows[i].Cells[0].Value.ToString());
                int neuron = Int32.Parse(dataGridView1.Rows[i].Cells[1].Value.ToString());
                int weight = Int32.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString());

                //если стоит галочка и до этого момента нейрон не отключался, значит отключаем нейрон и записываем его вес во временный массив,
                //чтобы потом можно было обратно включить нейрон
                if ((this.dataGridView1.Rows[i].Cells[4].Value.ToString() == "T".ToString()) &&
                    network.Layers[layer].Neurons[neuron].Weights[weight] != 0.0)
                {
                    tempWeights[layer][neuron][weight][0] = network.Layers[layer].Neurons[neuron].Weights[weight];
                    network.Layers[layer].Neurons[neuron].Weights[weight] = 0.0;
                    this.dataGridView1.Rows[i].Cells[3].Value = 0.0.ToString();
                }
                else {
                    //если галочка не стоит, и вес этого нейрона записан в временном массиве,
                    //значит он был отключен, а сейчас его нужно включить
                    if ((this.dataGridView1.Rows[i].Cells[4].Value.ToString() == "F".ToString()) && tempWeights[layer][neuron][weight][0] != 0.0)
                    {
                        network.Layers[layer].Neurons[neuron].Weights[weight] = tempWeights[layer][neuron][weight][0];
                        tempWeights[layer][neuron][weight][0] = 0.0;
                        this.dataGridView1.Rows[i].Cells[3].Value = network.Layers[layer].Neurons[neuron].Weights[weight].ToString();
                    }
                }
            }

            draw();
        }

        /**
         * Запуск потока загрузки нейронной сети
         * */
        private void LoadNetToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Worker = new Thread(LoadNet);
            Worker.SetApartmentState(ApartmentState.STA);
            Worker.Start();
            
        }

        /**
         * Загрузка нейронной сети
         * */
        private void LoadNet()
        {
            // Initialize the OpenFileDialog to look for text files.
            openFileDialog1.Filter = "Bin Files|*.bin";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    network = Network.Load(openFileDialog1.FileName);

                    //подсчет нейронов в сети
                    int neuronsCount = 0;
                    for (int layer = 0; layer < network.Layers.Length; layer++)
                    {
                        for (int neuron = 0; neuron < network.Layers[layer].Neurons.Length; neuron++)
                        {
                            neuronsCount++;
                        }
                    }

                    this.neuronsCountBox.Invoke(new Action<String>((nCount) => neuronsCountBox.Text = nCount), neuronsCount.ToString()); 
                }
                catch (IOException)
                {
                    throw new IOException("Ошибка загрузки нейронной сети");
                }
                finally
                {
                    this.Invoke(new Action(InitWork));
                    this.Invoke(new Action(draw));
                    Worker.Abort();
                }
            }
        }


        /**
         * Инициализация компонентов для работы
         * */
        private void InitWork()
        {
            this.setNeuronsDataGrid();

            tempWeights = new double[network.Layers.Length][][][];
            for (int i = 0; i < network.Layers.Length; i++)
            {
                tempWeights[i] = new double[network.Layers[i].Neurons.Length][][];
                for (int j = 0; j < network.Layers[i].Neurons.Length; j++)
                {
                    tempWeights[i][j] = new double[network.Layers[i].Neurons[j].Weights.Length][];
                    for (int k = 0; k < network.Layers[i].Neurons[j].Weights.Length; k++)
                    {
                        tempWeights[i][j][k] = new double[1];
                        tempWeights[i][j][k][0] = network.Layers[i].Neurons[j].Weights[k];
                    }
                }
            }
        }

        /**
         * Загрузка выборки
         * */
        private void loadTestData()
        {

            if (this.selectedType== "classification")
            {
                this.getDataForClass();
            }
            else if (this.selectedType== "regression")
            {
                this.getDataForRegression();
            }
        }

        /**
         * Вызов потока загрузки выборки
         * */
        private void LoadDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Worker = new Thread(loadTestData);
            Worker.SetApartmentState(ApartmentState.STA);
            Worker.Start();
            this.testNetButton.Enabled = true;
        }


        private void testing()
        {
  
            if (this.selectedType == "classification")
            {
                //subnet for correcting crash neural net
                //###################################
                Subnet[] subnets = new Subnet[1];
                if (this.checkBox1.Checked)
                {
                    String[] temp = this.hiddenTopology.Text.Split(',');
                    List<int> hiddenNeuronsAndLayers = new List<int>();
                    /*if (temp.Length < 1)
                        throw new Exception();*/
                    for (int i = 0; i < temp.Length; i++)
                    {
                        hiddenNeuronsAndLayers.Add(int.Parse(temp[i]));
                    }
                    //add output layer
                    hiddenNeuronsAndLayers.Add(1);
                    
                    subnets[0] = new Subnet(connectedNeurons.Count, hiddenNeuronsAndLayers.ToArray());

                    Hashtable inputRelations = new Hashtable();
                    int neuron = 0;
                    foreach (string connected in connectedNeurons)
                    {
                        inputRelations.Add(neuron, connected);
                        neuron++;
                    }
                    subnets[0].setInputAssosiated(inputRelations);

                    Hashtable outputRelations = new Hashtable();
                    outputRelations.Add(0, "1:0");
                    outputRelations.Add(1, "1:1");
                    subnets[0].setOutputAssosiated(outputRelations);
                }
                //####################################

                double[] res;
                try
                {
                    double[] input = new double[colCountData - 1];
                    validateError = 0.0;
                    for (int count = 0; count < data.GetLength(0) - 1; count++)
                    {
                        //gather inputs for compute, n-1 inputs
                        for (int i = 0; i < colCountData - 1; i++)
                        {
                            input[i] = data[count, i];
                        }
                        res = network.Compute(input, subnets);
                        double value = Math.Abs(1 - res[classesList.IndexOf(classes[count])]);
                        if (value > 0.0001)
                        {
                            validateError += value;
                        }

                    }
                    this.testQuality = (1 - (validateError / data.GetLength(0))) * 100;
                    this.errorTextBox.Text = this.testQuality.ToString("F10");
                }
                catch (Exception)
                {
                    MessageBox.Show("Ошибка тестирования сети.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            else if (this.selectedType == "regression")
            {
                double testError = 0.0;
                double[] input = new double[colCountData - 1];

                for (int i = 0; i < data.GetLength(0); i++)
                {
                    //gather inputs for compute, n-1 inputs
                    for (int j = 0; j < colCountData - 1; j++)
                    {
                        input[j] = data[i, j];
                    }
                    testError += Math.Abs(network.Compute(input)[0] - data[i, colCountData - 1]);
                }
                this.errorTextBox.Text = (testError / data.GetLength(0)).ToString("F10");


            }
 
        }
        /**
         * Процент ошибки при тестировании на выбранной выборке
         * */
        private void testNetButton_Click(object sender, EventArgs e)
        {
            this.CheckNeurons();
            this.testing();
                
        }
        //load data for classification
        private void getDataForClass()
        {
            // show file selection dialog
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                StreamReader reader = null;
                int i = 0;
                try
                {
                    // open selected file
                    reader = File.OpenText(openFileDialog2.FileName);

                    //get row count values
                    String line;
                    rowCountData = 0;
                    colCountData = 0;

                    //get input and output count
                    line = reader.ReadLine();
                    rowCountData++;
                    colCountData = line.Trim().Split(' ').Length;

                    //mass for new normalization data
                    double[] minData = new double[colCountData];
                    double[] maxData = new double[colCountData];

                    //must be > 1 column in training data
                    if (colCountData == 1)
                        throw new Exception();

                    while ((line = reader.ReadLine()) != null)
                    {
                        rowCountData++;
                    }

                    double[,] tempData = new double[rowCountData, colCountData];
                    int[] tempClasses = new int[rowCountData];

                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    line = "";

                    samplesPerClass = new int[2000];

                    // read the data
                    classesList.Clear();
                    while ((i < rowCountData) && ((line = reader.ReadLine()) != null))
                    {
                        string[] strs = line.Trim().Split(' ');
                        // parse input and output values for learning
                        //gather all input by cols
                        for (int j = 0; j < colCountData - 1; j++)
                        {
                            tempData[i, j] = double.Parse(strs[j]);

                            //search min/max values for each columnt
                            if (tempData[i, j] < minData[j])
                                minData[j] = tempData[i, j];
                            if (tempData[i, j] > maxData[j])
                                maxData[j] = tempData[i, j];
                        }

                        if (strs.Length - 1 < colCountData - 1)
                            continue;
                        tempClasses[i] = int.Parse(strs[colCountData - 1]);

                        //insert class in list of classes, if not find
                        if (classesList.IndexOf(tempClasses[i]) == -1)
                        {
                            classesList.Add(tempClasses[i]);
                        }

                        samplesPerClass[tempClasses[i]]++;

                        i++;
                    }

                    for (int row = 0; row < rowCountData; row++)
                    {
                        for (int column = 0; column < colCountData; column++)
                        {
                            tempData[row, column] = (((tempData[row, column] - minData[column]) * 1 / (maxData[column] - minData[column])));

                        }
                    }

                    // allocate and set data
                    data = new double[i, colCountData];
                    Array.Copy(tempData, 0, data, 0, i * colCountData);
                    classes = new int[i];
                    Array.Copy(tempClasses, 0, classes, 0, i);

                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Ошибка на  " + i.ToString() + " строке", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    // close file
                    if (reader != null)
                        reader.Close();
                }

            }
        }

        //load data for regression
        private void getDataForRegression()
        {
            // show file selection dialog
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                StreamReader reader = null;

                try
                {
                    // open selected file
                    reader = File.OpenText(openFileDialog2.FileName);

                    //get row count values
                    String line;
                    rowCountData = 0;
                    colCountData = 0;

                    //get input and output count
                    line = reader.ReadLine();
                    rowCountData++;
                    colCountData = line.Split(';').Length;

                    //must be > 1 column in training data
                    if (colCountData == 1)
                        throw new Exception();

                    while ((line = reader.ReadLine()) != null)
                    {
                        rowCountData++;
                    }

                    double[,] tempData = new double[rowCountData, colCountData];

                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    line = "";
                    int i = 0;

                    // read the data
                    while ((i < rowCountData) && ((line = reader.ReadLine()) != null))
                    {
                        string[] strs = line.Split(';');
                        // parse input and output values for learning
                        //gather all values by cols
                        for (int j = 0; j < colCountData; j++)
                        {
                            tempData[i, j] = double.Parse(strs[j]);
                        }

                        i++;
                    }

                    // allocate and set data
                    data = new double[i, colCountData];
                    Array.Copy(tempData, 0, data, 0, i * colCountData);

                }
                catch (Exception)
                {
                    MessageBox.Show("Ошибка чтения файла", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    // close file
                    if (reader != null)
                        reader.Close();
                }

            }
            this.testNetButton.Invoke(new Action(() => testNetButton.Enabled = true));

        }


        /**
         * Запуск автоматическго отключения нейронов
         * */
        private void startOffNeuronsButton_Click(object sender, EventArgs e)
        {
            int begin = 0;
            int end = 0;
            try
            {
                string[] range = this.neuronsToOffBox.Text.Split('-');
                begin = int.Parse(range[0]);
                end = int.Parse(range[1]);
            }
            catch(Exception exc) 
            {
                throw new Exception("Неверно введен диапазон.", exc);
            }

            //перебор сети и отключение нейронов
            int neuronsCount = 0;

            int beginLayer = 0;
            int beginNeuron = 0;

            //поиск начала диапазона в нейронной сети
            for (int layer = 0; layer < network.Layers.Length; layer++)
            {
                for (int neuron = 0; neuron < network.Layers[layer].Neurons.Length; neuron++)
                {
                    if (neuronsCount == begin)
                    {
                        beginLayer = layer;
                        beginNeuron = neuron;
                    }
                    neuronsCount++;
                }
            }

            int endLayer = 0;
            int endNeuron = 0;
            neuronsCount = 0;
            //поиск конца диапазона в нейронной сети
            for (int layer = 0; layer < network.Layers.Length; layer++)
            {
                for (int neuron = 0; neuron < network.Layers[layer].Neurons.Length; neuron++)
                {
                    if (neuronsCount == end)
                    {
                        endLayer = layer;
                        endNeuron = neuron;
                    }
                    neuronsCount++;
                }
            }

            try
            {
                if ((beginNeuron + beginLayer) > (endNeuron + endLayer))
                {
                    throw new Exception();
                }
                
            }
            catch
            {
                MessageBox.Show("Начало диапазона не может быть больше конца диапазона.", "Ошибка!");
                return;
            }
            offNeurons(beginLayer, endLayer, beginNeuron, endNeuron, (end - begin) + 1);

        }

        private void offNeurons(int beginLayer, int endLayer, int beginNeuron, int endNeuron, int acount)
        {
            string time = DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss");

            fileXlsNeurons = "C:\\Neurons" + time + ".xls";
            workbook = new Workbook();
            worksheet = new Worksheet("First Sheet");
            fileXlsWeightsRel = "C:\\WeightsRel" + time + ".xls";
            workbook2 = new Workbook();
            worksheet2 = new Worksheet("First Sheet");
            fileXlsWeightsAbs = "C:\\WeightsAbs" + time + ".xls";
            workbook3 = new Workbook();
            worksheet3 = new Worksheet("First Sheet");
            //глобальный счетчик выделенных в диапазон нейронов
            int count = 0;
            //массив выделенных нейронов
            Record[] rangeNeurons = new Record[acount];

            //проход по вычисленным из диапазона слоям
            for (int layer = beginLayer; layer <= endLayer; layer++)
            {
                // если текущий слой первый и не последний из выделенного диапазона
                if ((layer == beginLayer) && (layer != endLayer))
                {   
                    //собираем нейроны от первого нейрона, до конца слоя
                    for (int neurons = beginNeuron; neurons < network.Layers[layer].Neurons.Length; neurons++)
                    {
                        rangeNeurons[count] = new Record(layer, neurons);
                        count++;
                    }
                }

                //если слой первый и последний
                if ((layer == beginLayer) && (layer == endLayer))
                {
                    //собираем нейроны из заданного диапазона
                    for (int neurons = beginNeuron; neurons <= endNeuron; neurons++)
                    {
                        rangeNeurons[count] = new Record(layer, neurons);
                        count++;
                    }
                }

                //если слой последний и не первый
                if ((layer == endLayer) && (layer != beginLayer))
                {
                    //собираем нейроны от начала слоя и до выделенного из диапазона нейрона
                    for (int neurons = 0; neurons <= endNeuron; neurons++)
                    {
                        rangeNeurons[count] = new Record(layer, neurons);
                        count++;
                    }
                }

                //если слой не первый и не последний
                if ((layer != beginLayer) && (layer != endLayer))
                {
                    //собираем тупо нейроны всего слоя
                    for (int neurons = 0; neurons < network.Layers[layer].Neurons.Length; neurons++)
                    {
                        rangeNeurons[count] = new Record(layer, neurons);
                        count++;
                    }
                }
            }


            //write headers to xls
            worksheet = this.setNeuronsWorkBookHeaders(worksheet, rangeNeurons);
            worksheet2 = this.setWeightsWorkBookHeaders(worksheet2, rangeNeurons);
            worksheet3 = this.setWeightsWorkBookHeaders(worksheet3, rangeNeurons);

            int combinations = 1;
            String verticalGroup = "";
            int fixedOffNeurons = 0;
            double allWeightsOfNet = 0;
            //TO Function
            for (int layer = 0; layer < network.Layers.Length; layer++ )
            {
                for (int neuro = 0; neuro < network.Layers[layer].Neurons.Length; neuro++)
                {
                    for (int wei = 0; wei < network.Layers[layer].Neurons[neuro].Weights.Length; wei++)
                    {
                        allWeightsOfNet += network.Layers[layer].Neurons[neuro].Weights[wei];
                    }
                }
            }


            connectedNeurons = new List<string>();
            for (int k = 0; k < rangeNeurons.Length-2; k++)
            {
                for (int currentNeuron = 0; currentNeuron < rangeNeurons.Length; currentNeuron++)
                {
                    //batch neurons off
                    for (int off = currentNeuron; off <= currentNeuron + k; off++)
                    {
                        if (off > rangeNeurons.Length - 1)
                        {
                            offWeights(network, rangeNeurons[off - rangeNeurons.Length]);
                            verticalGroup += rangeNeurons[off - rangeNeurons.Length].numberLayer.ToString() + ":" + rangeNeurons[off - rangeNeurons.Length].numberNeuron.ToString();
                            connectedNeurons.Add(rangeNeurons[off - rangeNeurons.Length].numberLayer.ToString() + ":" + rangeNeurons[off - rangeNeurons.Length].numberNeuron.ToString());
                        }
                        else
                        {
                            offWeights(network, rangeNeurons[off]);
                            verticalGroup += rangeNeurons[off].numberLayer.ToString() + ":" + rangeNeurons[off].numberNeuron.ToString();
                            connectedNeurons.Add(rangeNeurons[off].numberLayer.ToString() + ":" + rangeNeurons[off].numberNeuron.ToString());
                        }
                        verticalGroup += "|";
                        fixedOffNeurons++;

                    }
                    
                    worksheet.Cells[combinations,0] = new Cell(verticalGroup);
                    worksheet2.Cells[combinations, 0] = new Cell(verticalGroup);
                    worksheet3.Cells[combinations, 0] = new Cell(verticalGroup);

                    int step = 1;
                    for (int j = 0; j < rangeNeurons.Length; j++, step += 2)
                    {
                        if ((j >= currentNeuron) && (j <= currentNeuron + k))
                            continue;
                        if ((j <= ((currentNeuron + k) - rangeNeurons.Length)) && (j >= (currentNeuron - rangeNeurons.Length)))
                            continue;

                        offWeights(network, rangeNeurons[j]);
                        connectedNeurons.Add(rangeNeurons[j].numberLayer.ToString() + ":" + rangeNeurons[j].numberNeuron.ToString());
                        this.testing();

                        worksheet.Cells[combinations, j+1] = new Cell(this.testQuality);
                        worksheet2.Cells[combinations, step] = new Cell(this.offWeightsSumInput.ToString("F2"));
                        worksheet2.Cells[combinations, step + 1] = new Cell(this.offWeightsSumOutput.ToString("F2"));
                        worksheet3.Cells[combinations, step] = new Cell(this.offWeightsSumAbsoluteInput.ToString("F2"));
                        worksheet3.Cells[combinations, step + 1] = new Cell(this.offWeightsSumAbsoluteOutput.ToString("F2"));

                        onWeights(network, rangeNeurons[j]);
                        connectedNeurons.Remove(rangeNeurons[j].numberLayer.ToString() + ":" + rangeNeurons[j].numberNeuron.ToString());
                    }
                    //batch neurons to On
                    for (int off = currentNeuron; off <= currentNeuron + k; off++)
                    {
                        if (off > rangeNeurons.Length - 1)
                        {
                            onWeights(network, rangeNeurons[off - rangeNeurons.Length]);
                            connectedNeurons.Remove(rangeNeurons[off - rangeNeurons.Length].numberLayer.ToString() + ":" + rangeNeurons[off - rangeNeurons.Length].numberNeuron.ToString());
                            //break;
                        }
                        else
                        {
                            onWeights(network, rangeNeurons[off]);
                            connectedNeurons.Remove(rangeNeurons[off].numberLayer.ToString() + ":" + rangeNeurons[off].numberNeuron.ToString());
                        }
                    }

                    worksheet.Cells[combinations, rangeNeurons.Length + 1] = new Cell(fixedOffNeurons + 1);
                    int count1 = getCountNeuronsOfNet();
                    double res = (double)(fixedOffNeurons + 1) / (double)getCountNeuronsOfNet();
                    worksheet.Cells[combinations, rangeNeurons.Length + 2] = new Cell(res.ToString());
                    worksheet.Cells[combinations, rangeNeurons.Length + 3] = new Cell((fixedOffNeurons + 1).ToString() + "/" + count1 );

                    combinations++;
                    verticalGroup = "";
                    fixedOffNeurons = 0;
                }
            }
            workbook.Worksheets.Add(worksheet);
            workbook.Save(fileXlsNeurons);
            workbook2.Worksheets.Add(worksheet2);
            workbook2.Save(fileXlsWeightsRel);
            workbook3.Worksheets.Add(worksheet3);
            workbook3.Save(fileXlsWeightsAbs);
            MessageBox.Show("Перебор окончен.");
        }

        private Worksheet setNeuronsWorkBookHeaders(Worksheet worksheet, Record[] rangeNeurons)
        {
            for (int header = 0; header < rangeNeurons.Length; header++)
            {
                worksheet.Cells[0, header + 1] = new Cell(rangeNeurons[header].numberLayer.ToString() + ":" + rangeNeurons[header].numberNeuron.ToString());
            }
            worksheet.Cells[0, rangeNeurons.Length + 1] = new Cell("Число отключенных нейронов");
            worksheet.Cells[0, rangeNeurons.Length + 2] = new Cell("ЧОН/Нейронов всего");
            worksheet.Cells[0, rangeNeurons.Length + 3] = new Cell("Соотношение нейронов");
            return worksheet;
        }

        private Worksheet setWeightsWorkBookHeaders(Worksheet worksheet, Record[] rangeNeurons)
        {
            int step = 1;
            for (int header = 0; header < rangeNeurons.Length; header++)
            {
                
                worksheet.Cells[0, step] = new Cell(rangeNeurons[header].numberLayer.ToString() + ":" + rangeNeurons[header].numberNeuron.ToString() + "|Вх.");
                worksheet.Cells[0, step + 1] = new Cell(rangeNeurons[header].numberLayer.ToString() + ":" + rangeNeurons[header].numberNeuron.ToString() + "|Исх.");
                step += 2;
            }
            return worksheet;
 
        }

        private int getCountNeuronsOfNet()
        {
            int count = 0;
            for (int i = 0; i < network.Layers.Length; i++)
            {
                for (int j = 0; j < network.Layers[i].Neurons.Length; j++)
                {
                    count++;
                }
            }
            return count;
        }

        //включение весов
        private void onWeights(Network network, Record currentNeuron)
        {
            for (int weight = 0; weight < network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights.Length; weight++)
            {
                network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights[weight] = tempWeights[currentNeuron.numberLayer][currentNeuron.numberNeuron][weight][0];
                tempWeights[currentNeuron.numberLayer][currentNeuron.numberNeuron][weight][0] = 0.0;
                this.offWeightsSumInput -= network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights[weight];
                this.offWeightsSumAbsoluteInput -= Math.Abs(network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights[weight]);
            }

            //if next layer is
            if ((network.Layers.Length - 1) >= (currentNeuron.numberLayer + 1))
            {
                for (int i = 0; i < network.Layers[currentNeuron.numberLayer + 1].Neurons.Length; i++)
                {
                    this.offWeightsSumOutput -= network.Layers[currentNeuron.numberLayer + 1].Neurons[i].Weights[currentNeuron.numberNeuron];
                    this.offWeightsSumAbsoluteOutput -= Math.Abs(network.Layers[currentNeuron.numberLayer + 1].Neurons[i].Weights[currentNeuron.numberNeuron]);
                }
            }

        }

        //отключение весов
        private void offWeights(Network network, Record currentNeuron)
        {
            for (int weights = 0; weights < network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights.Length; weights++)
            {
                
                tempWeights[currentNeuron.numberLayer][currentNeuron.numberNeuron][weights][0] = network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights[weights];
                network.Layers[currentNeuron.numberLayer].Neurons[currentNeuron.numberNeuron].Weights[weights] = 0.0;
                this.offWeightsSumInput += tempWeights[currentNeuron.numberLayer][currentNeuron.numberNeuron][weights][0];
                this.offWeightsSumAbsoluteInput += Math.Abs(tempWeights[currentNeuron.numberLayer][currentNeuron.numberNeuron][weights][0]);
            }
            //if next layer is
            if ((network.Layers.Length - 1) >= (currentNeuron.numberLayer + 1))
            {
                for (int i = 0; i < network.Layers[currentNeuron.numberLayer + 1].Neurons.Length; i++ )
                {
                    this.offWeightsSumOutput += network.Layers[currentNeuron.numberLayer + 1].Neurons[i].Weights[currentNeuron.numberNeuron];
                    this.offWeightsSumAbsoluteOutput += Math.Abs(network.Layers[currentNeuron.numberLayer + 1].Neurons[i].Weights[currentNeuron.numberNeuron]);
                }
            }
        }

        private void FormDrawNeurons_Load(object sender, EventArgs e)
        {
            HelloForm form = this.Owner as HelloForm;
            this.selectedType = form.typeLearn;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

    }

    class Record
    {
        public int numberLayer;
        public int numberNeuron;
        public Record(int layer, int neuron)
        {
            numberLayer = layer;
            numberNeuron = neuron;
        }
    }

}
