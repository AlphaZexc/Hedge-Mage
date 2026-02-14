using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

namespace BookCurlPro
{
    [RequireComponent(typeof(BookPro))]
    public class AutoFlip : MonoBehaviour
    {
        public BookPro ControledBook;
        public FlipMode Mode;
        public float singlePageFlip = 1;
        public float multiPageFlip = 0.1f;
        public float DelayBeforeStart;
        public float TimeBetweenPages = 5;
        public bool AutoStartFlip = true;
        bool flippingStarted = false;
        bool isPageFlipping = false;
        float elapsedTime = 0;
        float nextPageCountDown = 0;
        bool isBookInteractable;
        // Use this for initialization
        void Start()
        {
            if (!ControledBook)
                ControledBook = GetComponent<BookPro>();

            if (AutoStartFlip)
                StartFlipping(ControledBook.EndFlippingPaper + 1);
        }
        public void FlipRightPage(float flipTime, int numPages = 1)
        {
            for (int i = 0; i < numPages; i++)
            {
                if (isPageFlipping) return;
                if (ControledBook.CurrentPaper >= ControledBook.papers.Length) return;
                isPageFlipping = true;
                PageFlipper.FlipPage(ControledBook, flipTime, FlipMode.RightToLeft, () => { isPageFlipping = false; });
            }
        }
        public void FlipLeftPage(float flipTime, int numPages = 1)
        {
            for (int i = 0; i < numPages; i++)
            {
                if (isPageFlipping) return;
                if (ControledBook.CurrentPaper <= 0) return;
                isPageFlipping = true;
                PageFlipper.FlipPage(ControledBook, flipTime, FlipMode.LeftToRight, () => { isPageFlipping = false; });
            }
        }
        int targetPaper;
        public void StartFlipping(int target)
        {
            isBookInteractable = ControledBook.interactable;
            ControledBook.interactable = false;
            flippingStarted = true;
            elapsedTime = 0;
            nextPageCountDown = 0;
            targetPaper = target;
            if (target > ControledBook.CurrentPaper) Mode = FlipMode.RightToLeft;
            else if (target < ControledBook.currentPaper) Mode = FlipMode.LeftToRight;
        }

        public void GotoPage(int pageNum)
        {
            if (pageNum < 0) pageNum = 0;
            if (pageNum > ControledBook.papers.Length * 2) pageNum = ControledBook.papers.Length * 2 - 1;
            TimeBetweenPages = 0;
            StartFlipping((pageNum + 1) / 2);
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.RightArrow))
            {
                FlipRightPage(singlePageFlip);
            } else if (Input.GetKey(KeyCode.LeftArrow))
            {
                FlipLeftPage(singlePageFlip);
            }

            if (flippingStarted)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > DelayBeforeStart)
                {
                    if (nextPageCountDown < 0)
                    {
                        if ((ControledBook.CurrentPaper < targetPaper &&
                            Mode == FlipMode.RightToLeft) ||
                            (ControledBook.CurrentPaper > targetPaper &&
                            Mode == FlipMode.LeftToRight))
                        {
                            isPageFlipping = true;
                            PageFlipper.FlipPage(ControledBook, multiPageFlip, Mode, () => { isPageFlipping = false; });
                        }
                        else
                        {
                            flippingStarted = false;
                            ControledBook.interactable = isBookInteractable;
                            this.enabled = false;

                        }

                        nextPageCountDown = multiPageFlip + TimeBetweenPages + Time.deltaTime;
                    }
                    nextPageCountDown -= Time.deltaTime;
                }
            }
        }
    }
}