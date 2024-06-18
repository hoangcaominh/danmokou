﻿using System.Collections.Generic;
using BagoumLib;
using BagoumLib.Tasks;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.UI.XML;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Danmokou.UI {
public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler {
    public Canvas? canvas;
    public TMP_Text text = null!;
    private readonly Dictionary<string, UIGroup> tooltips = new();

    public void ClearTooltips() {
        foreach (var tt in tooltips.Values)
            _ = tt.LeaveGroup().ContinueWithSync();
        tooltips.Clear();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button == PointerEventData.InputButton.Right) {
            ClearTooltips();
            return;
        }
        var li = FindIntersectingLink(text, eventData.position, canvas == null ? null! : canvas.worldCamera,
            out _, out var tl, out _, out var tr);
        Logs.Log($"Clicked on {gameObject.name}, {eventData.position}, li {li}");
        if (li < 0) {
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.pointerClickHandler);
            return;
        }
        var link = text.textInfo.linkInfo[li];
        var lid = link.GetLinkID();
        if (tooltips.Remove(lid, out var ett)) {
            _ = ett.LeaveGroup().ContinueWithSync();
        } else {
            var (succ, tt) = LinkCallback.ProcessClick(lid);
            if (!succ) {
                Logs.Log($"No processor found for link of id {lid}", level: LogLevel.WARNING);
                return;
            }
            if (tt != null) {
                tt.Render.HTML.WithAbsolutePosition(UIBuilderRenderer.ToXMLPos((tl + tr) / 2.0f));
                tooltips[lid] = tt;
            }
        }
    }

    public void OnPointerMove(PointerEventData eventData) {
        /*Debug.Log($"POINTER MVOE {eventData}");
        var li = TMP_TextUtilities.FindIntersectingLink(text, eventData.position,
            canvas == null ? null! : canvas.worldCamera);
        Debug.Log(li);*/
    }


    void OnEnable() {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    void OnDisable() {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
    }

    private void ON_TEXT_CHANGED(Object obj) {
        if (obj != text) return;
        ClearTooltips();
    }


    /// <summary>
    /// Function returning the index of the Link at the given position (if any).
    /// </summary>
    /// <param name="text">A reference to the TMP_Text component.</param>
    /// <param name="position">Position to check for intersection.</param>
    /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
    /// <param name="bl">Bottom-left of the rectangle of the word matching the link.</param>
    /// <param name="tl">Top-left of the rectangle of the word matching the link.</param>
    /// <param name="br">Bottom-right of the rectangle of the word matching the link.</param>
    /// <param name="tr">Top-right of the rectangle of the word matching the link.</param>
    /// <returns></returns>
    public static int FindIntersectingLink(TMP_Text text, Vector3 position, Camera camera,
        out Vector3 bl, out Vector3 tl, out Vector3 br, out Vector3 tr) {
        Transform rectTransform = text.transform;

        // Convert position into Worldspace coordinates
        TMP_TextUtilities.ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

        bl = Vector3.zero;
        tl = Vector3.zero;
        br = Vector3.zero;
        tr = Vector3.zero;

        for (int i = 0; i < text.textInfo.linkCount; i++) {
            TMP_LinkInfo linkInfo = text.textInfo.linkInfo[i];

            bool isBeginRegion = false;

            // Iterate through each character of the word
            for (int j = 0; j < linkInfo.linkTextLength; j++) {
                int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                TMP_CharacterInfo currentCharInfo = text.textInfo.characterInfo[characterIndex];
                int currentLine = currentCharInfo.lineNumber;

                // Check if Link characters are on the current page
                if (text.overflowMode == TextOverflowModes.Page &&
                    currentCharInfo.pageNumber + 1 != text.pageToDisplay) continue;

                if (isBeginRegion == false) {
                    isBeginRegion = true;

                    bl = rectTransform.TransformPoint(new Vector3(currentCharInfo.bottomLeft.x,
                        currentCharInfo.descender, 0));
                    tl = rectTransform.TransformPoint(new Vector3(currentCharInfo.bottomLeft.x,
                        currentCharInfo.ascender, 0));

                    //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                    // If Word is one character
                    if (linkInfo.linkTextLength == 1) {
                        isBeginRegion = false;

                        br = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x,
                            currentCharInfo.descender, 0));
                        tr = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x,
                            currentCharInfo.ascender, 0));

                        // Check for Intersection
                        if (PointIntersectRectangle(position, bl, tl, tr, br))
                            return i;

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                }

                // Last Character of Word
                if (isBeginRegion && j == linkInfo.linkTextLength - 1) {
                    isBeginRegion = false;

                    br = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x, currentCharInfo.descender,
                        0));
                    tr = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender,
                        0));

                    // Check for Intersection
                    if (PointIntersectRectangle(position, bl, tl, tr, br))
                        return i;

                    //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                }
                // If Word is split on more than one line.
                else if (isBeginRegion && currentLine != text.textInfo.characterInfo[characterIndex + 1].lineNumber) {
                    isBeginRegion = false;

                    br = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x, currentCharInfo.descender,
                        0));
                    tr = rectTransform.TransformPoint(new Vector3(currentCharInfo.topRight.x, currentCharInfo.ascender,
                        0));

                    // Check for Intersection
                    if (PointIntersectRectangle(position, bl, tl, tr, br))
                        return i;
                }
            }
        }

        return -1;
    }

    private static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
        Vector3 ab = b - a;
        Vector3 am = m - a;
        Vector3 bc = c - b;
        Vector3 bm = m - b;

        float abamDot = Vector3.Dot(ab, am);
        float bcbmDot = Vector3.Dot(bc, bm);

        return 0 <= abamDot && abamDot <= Vector3.Dot(ab, ab) && 0 <= bcbmDot && bcbmDot <= Vector3.Dot(bc, bc);
    }
}
}