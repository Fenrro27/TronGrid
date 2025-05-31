using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RecordsController : MonoBehaviour
{
    [System.Serializable]
    public struct RecordData { 
        public string nombreJugador;
        public int puntuacion;
    }

    [System.Serializable]
    public class Records {
        public List<RecordData> data;
    }

    public Records records;
    public string archivoDeRecords;

    public RectTransform panelTextos;

    private void Awake() {
        archivoDeRecords = Application.persistentDataPath + "/records.json";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
            cargarRecords();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void cargarRecords()
    {
        if (File.Exists(archivoDeRecords))
        {
            string recordsData = File.ReadAllText(archivoDeRecords);
            records = JsonUtility.FromJson<Records>(recordsData);

            foreach (RecordData record in records.data)
            {
                panelTextos.GetChild(records.data.IndexOf(record)).GetComponent<TextMeshProUGUI>().text = record.nombreJugador + " - - - - - " + record.puntuacion;
            }
        }
        else {
            Debug.Log("El archivo de records no existe");
        }
    }
    
    public void guardarRecord(string nombreJugador, int puntuacion) {
        string data;
        RecordData record;
        record.nombreJugador = nombreJugador;
        record.puntuacion = puntuacion;

        if (File.Exists(archivoDeRecords))
        {
            data = File.ReadAllText(archivoDeRecords);
            records = JsonUtility.FromJson<Records>(data);
        }
        else
        {
            Debug.Log("El archivo de records no existe");
        }

        if (!records.data.Contains(record))
        {
            records.data.Add(record);
            records.data.Sort((p1, p2) => p2.puntuacion.CompareTo(p1.puntuacion));

            if (records.data.Count > 10)
                records.data.RemoveAt(10);
        }

        data = JsonUtility.ToJson(records);
        File.WriteAllText(archivoDeRecords, data);

    }
}
