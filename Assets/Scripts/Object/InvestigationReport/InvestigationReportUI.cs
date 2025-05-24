using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Object.InvestigationReport
{
    public class InvestigationReportUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DetectableGameObject detectableGameObject;
        [SerializeField] private GameObject investigationReportUI;
        [SerializeField] private GameObject itemList; // Conteneur principal pour les grilles
        [SerializeField] private GameObject investigationReportItemPrefab;
        [SerializeField] private GameObject gridLayoutGroupPrefab; // Nouveau prefab pour la grille
        [SerializeField] private CursorManager cursorManager;
        [SerializeField] private UI.Carousel.Carousel2D carousel; // Référence au carousel
        
        [Header("Configuration")]
        [SerializeField] private int maxReportItems = 9;
        [SerializeField] private int itemsPerGrid = 4; // Nombre d'items par grille
        
        private bool isShowing = false;
        private List<InvestigationData> investigationReportItems = new List<InvestigationData>();
        private Dictionary<string, GameObject> uiItemsMap = new Dictionary<string, GameObject>();
        private List<GameObject> gridContainers = new List<GameObject>();
        private bool cursorStateBeforeShow = false;

        #region Unity Callbacks

        void Start()
        {
            investigationReportUI.SetActive(false);
            MidiBinding.Instance.Subscribe(MidiBind.BUTTON_1_CUE_1, (input) => DisplayReport());
            MidiBinding.Instance.Subscribe(MidiBind.BUTTON_1_CUE_2, (input) => DisplayReport());
            MidiBinding.Instance.Subscribe(MidiBind.BUTTON_1_ROLL_1, (input) => DisplayReport());
            MidiBinding.Instance.Subscribe(MidiBind.BUTTON_1_ROLL_2, (input) => DisplayReport());
            
            // Trouver le CursorManager s'il n'est pas assigné
            if (cursorManager == null)
                cursorManager = FindObjectOfType<CursorManager>();
                
            // Trouver le Carousel s'il n'est pas assigné
            if (carousel == null)
                carousel = GetComponentInChildren<UI.Carousel.Carousel2D>();
        }

        private void Update()
        {
            // Si la touche I est pressée, simuler l'appui sur le bouton du controller
            if (Input.GetKeyDown(KeyCode.I))
            {
                DisplayReport();
            }
        
            // Vérifier si l'UI est affichée mais que l'objet n'est plus regardé
            if (isShowing && !detectableGameObject.isLookingAt)
            {
                HideUI();
            }
        }

        #endregion


        void ShowUI()
        {
            // Sauvegarder l'état actuel du curseur avant d'afficher l'UI
            if (cursorManager != null)
            {
                cursorStateBeforeShow = cursorManager.IsCursorHidden();
                
                // Désactiver la fonctionnalité de clic pour cacher le curseur
                cursorManager.SetHideOnClickEnabled(false);
                
                // Afficher le curseur quand la fiche est affichée
                cursorManager.SetCursorHidden(false);
            }
            
            investigationReportUI.SetActive(true);
            isShowing = true;
            
            // Forcer la mise à jour du carousel
            if (carousel != null)
            {
                carousel.ForceUpdate();
            }
        }

        void HideUI()
        {
            investigationReportUI.SetActive(false);
            isShowing = false;
            
            // Restaurer l'état précédent du curseur quand la fiche est cachée
            if (cursorManager != null)
            {
                // Réactiver la fonctionnalité de clic pour cacher le curseur
                cursorManager.SetHideOnClickEnabled(true);
                
                // Restaurer l'état précédent du curseur
                cursorManager.SetCursorHidden(cursorStateBeforeShow);
            }
        }

        void DisplayReport()
        {
            if (detectableGameObject.isLookingAt && !isShowing)
            {
                ShowUI();
            }
            else if (isShowing)
            {
                HideUI();
            }
        }
        
        public void AddInfoToReport(InvestigationData data)
        {
            if (investigationReportItems.Count < maxReportItems)
            {
                if (investigationReportItems.Contains(data))
                {
                    Debug.Log("Item" + data.id + " with description " + data.description + " already in report");
                    return;
                }
                // Assign unique id
                data.id = Guid.NewGuid().ToString();
                
                // Add to list
                investigationReportItems.Add(data);
                
                // Add to UI
                AddUIItem(data);
                
                // Forcer la mise à jour du carousel
                if (carousel != null)
                {
                    carousel.ForceUpdate();
                }
            }
        }
        
        public void ClearReport()
        {
            // Supprimer tous les éléments UI
            foreach (var item in uiItemsMap.Values)
            {
                if (item != null)
                    Destroy(item);
            }
            
            // Supprimer toutes les grilles
            foreach (var grid in gridContainers)
            {
                if (grid != null)
                    Destroy(grid);
            }
            
            uiItemsMap.Clear();
            gridContainers.Clear();
            investigationReportItems.Clear();
            
            // Forcer la mise à jour du carousel
            if (carousel != null)
            {
                carousel.ForceUpdate();
            }
        }
        
        // Cette méthode est conservée mais n'est plus utilisée par les boutons
        public void RemoveInfoFromReport(InvestigationData data)
        {
            if (!investigationReportItems.Contains(data))
            {
                Debug.Log("Item" + data.id + " with description " + data.description + " not found in report");
                return;
            }
            investigationReportItems.Remove(data);
            RemoveUIItem(data);
            
            // Réorganiser les items après suppression
            ReorganizeItems();
            
            // Forcer la mise à jour du carousel
            if (carousel != null)
            {
                carousel.ForceUpdate();
            }
        }
        
        #region UI Management

        private void AddUIItem(InvestigationData data)
        {
            // Déterminer dans quelle grille ajouter l'item
            int gridIndex = (investigationReportItems.Count - 1) / itemsPerGrid;
            int itemPositionInGrid = (investigationReportItems.Count - 1) % itemsPerGrid;
            
            // S'assurer que nous avons assez de grilles
            EnsureGridExists(gridIndex);
            
            // Obtenir la grille cible
            GameObject targetGrid = gridContainers[gridIndex];
            
            // Instancier l'item dans la grille
            var item = Instantiate(investigationReportItemPrefab, targetGrid.transform, false);
            
            if (!item.GetComponentInChildren<TextMeshProUGUI>())
            {
                Debug.LogError("Missing TextMeshProUGUI component on investigationReportItemPrefab");
                return;
            }
            item.GetComponentInChildren<TextMeshProUGUI>().text = data.description;

            item.name = data.id;
            
            // Stocker la référence à l'élément UI dans le dictionnaire
            uiItemsMap[data.id] = item;
            
            // Vérifier si nous avons besoin de mettre à jour le carousel
            CheckCarouselVisibility();
        }
        
        private void EnsureGridExists(int gridIndex)
        {
            // Créer de nouvelles grilles si nécessaire
            while (gridContainers.Count <= gridIndex)
            {
                GameObject newGrid = Instantiate(gridLayoutGroupPrefab, itemList.transform, false);
                newGrid.name = $"Grid_{gridContainers.Count}";
                
                // S'assurer que la grille est configurée pour 4 items
                GridLayoutGroup gridLayout = newGrid.GetComponent<GridLayoutGroup>();
                if (gridLayout != null)
                {
                    // Vérifier que la grille est configurée pour le bon nombre d'items par ligne
                    int columns = Mathf.CeilToInt(itemsPerGrid / 2.0f); // 2 lignes, donc divisé par 2
                    gridLayout.constraintCount = columns;
                }
                
                gridContainers.Add(newGrid);
            }
        }
        
        public void RemoveUIItem(InvestigationData data)
        {
            if (uiItemsMap.TryGetValue(data.id, out GameObject item))
            {
                if (item != null)
                    Destroy(item);
                
                uiItemsMap.Remove(data.id);
            }
            else
            {
                Debug.LogWarning("UI item with ID " + data.id + " not found in dictionary");
            }
            
            // Vérifier si nous avons besoin de mettre à jour le carousel
            CheckCarouselVisibility();
        }
        
        private void ReorganizeItems()
        {
            // Sauvegarder les données actuelles
            List<InvestigationData> tempItems = new List<InvestigationData>(investigationReportItems);
            
            // Effacer tous les éléments UI
            foreach (var item in uiItemsMap.Values)
            {
                if (item != null)
                    Destroy(item);
            }
            
            uiItemsMap.Clear();
            
            // Vider les grilles mais ne pas les détruire
            foreach (var grid in gridContainers)
            {
                foreach (Transform child in grid.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Vider la liste des items
            investigationReportItems.Clear();
            
            // Réajouter tous les items
            foreach (var data in tempItems)
            {
                // Réutiliser l'ID existant
                investigationReportItems.Add(data);
                
                // Déterminer dans quelle grille ajouter l'item
                int gridIndex = (investigationReportItems.Count - 1) / itemsPerGrid;
                int itemPositionInGrid = (investigationReportItems.Count - 1) % itemsPerGrid;
                
                // S'assurer que nous avons assez de grilles
                EnsureGridExists(gridIndex);
                
                // Obtenir la grille cible
                GameObject targetGrid = gridContainers[gridIndex];
                
                // Instancier l'item dans la grille
                var item = Instantiate(investigationReportItemPrefab, targetGrid.transform, false);
                
                if (item.GetComponentInChildren<TextMeshProUGUI>())
                {
                    item.GetComponentInChildren<TextMeshProUGUI>().text = data.description;
                }
                
                item.name = data.id;
                
                // Stocker la référence à l'élément UI dans le dictionnaire
                uiItemsMap[data.id] = item;
            }
            
            // Supprimer les grilles vides à la fin
            for (int i = gridContainers.Count - 1; i >= 0; i--)
            {
                if (gridContainers[i].transform.childCount == 0 && i == gridContainers.Count - 1)
                {
                    Destroy(gridContainers[i]);
                    gridContainers.RemoveAt(i);
                }
                else
                {
                    break; // Ne supprime que les grilles vides à la fin
                }
            }
            
            // Vérifier si nous avons besoin de mettre à jour le carousel
            CheckCarouselVisibility();
        }
        
        private void CheckCarouselVisibility()
        {
            // Mettre à jour le carousel si nous avons plus d'une grille
            if (carousel != null)
            {
                // Forcer la mise à jour du carousel
                carousel.ForceUpdate();
            }
        }

        #endregion
        
        private void OnDisable()
        {
            if (isShowing && cursorManager)
            {
                cursorManager.SetHideOnClickEnabled(true);
                cursorManager.SetCursorHidden(cursorStateBeforeShow);
            }
        }
    }
}
