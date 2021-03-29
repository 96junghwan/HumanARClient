using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;

namespace CellBig.Module.HumanDetection
{
    public static class ImagePreProcessor
    {
        /*
        // 이미지 Raw Data Byte를 JPG 압축 Byte로 변환하는 함수
        public static byte[] RawImageByte2JPG(byte[] src, Texture2D taskTexture, int resizeOption, int CamType)
        {
            byte[] dst;

            if (CamType == CameraType.OCam)
            {
                taskTexture.
            }

            else
            {

            }

            ImageResizer.

            return dst;
        }
        */

        // 안드로이드 전처리 : 이미지 480p 사이즈로 변경하는 함수
        public static byte[] ResizeAndroidImage480P(Texture2D resizingTexture, Texture2D resizedTexture, Mat resizingMat, Mat resizedMat)
        {
            Utils.fastTexture2DToMat(resizingTexture, resizingMat);
            Imgproc.resize(resizingMat, resizedMat, new Size(480, 640));
            Utils.fastMatToTexture2D(resizedMat, resizedTexture);
            return resizedTexture.EncodeToJPG();
        }

        // 이미지 480p 사이즈로 변경하는 함수
        public static byte[] ResizeImage480P(Texture2D resizingTexture, Texture2D resizedTexture, Mat resizingMat, Mat resizedMat)
        {
            Utils.fastTexture2DToMat(resizingTexture, resizingMat);
            Imgproc.resize(resizingMat, resizedMat, new Size(640, 480));
            Utils.fastMatToTexture2D(resizedMat, resizedTexture);
            return resizedTexture.EncodeToJPG();
        }

        // 이미지 720p 사이즈로 변경하는 함수
        public static byte[] ResizeImage720P(Texture2D resizingTexture, Texture2D resizedTexture, Mat resizingMat, Mat resizedMat)
        {
            Utils.fastTexture2DToMat(resizingTexture, resizingMat);
            Imgproc.resize(resizingMat, resizedMat, new Size(1280, 720));
            Utils.fastMatToTexture2D(resizedMat, resizedTexture);
            return resizedTexture.EncodeToJPG();
        }

        // 이미지 회전하는 함수 : 너무 날먹인가?
        public static void RotateImageMat(Mat src, Mat dst, int rotationOption)
        {
            Core.rotate(src, dst, rotationOption);
        }

        // Mat 전용 반전 함수 : 너무 날먹인가...?
        public static void FlipImageMat(Mat src, Mat dst, int flipOption)
        {
            Core.flip(src, dst, flipOption);
            //Mat mat = new Mat();
            //Imgcodecs
        }

        // OCam 전용 프레임 상하/좌우/상하좌우 반전시키는 함수
        public static void FlipImageByte(byte[] frameData, byte[] taskByte, int width, int height, FlipOption flipOption)
        {
            // flip용 변수
            int x = 0;
            int y = 0;
            int index;
            int flipIndex;

            // 잘못 들어온 경우 반환
            if (flipOption == FlipOption.NoFlip) { return; }

            // 이미지 바이트 카피
            Buffer.BlockCopy(frameData, 0, taskByte, 0, (width * height * 3));

            // 상하좌우 반전
            if (flipOption == FlipOption.DoubleFlip)
            {
                for (int i = 0; i < frameData.Length; i += 3)
                {
                    index = i / 3;

                    x = width - (index % width) - 1;
                    y = height - (index / width) - 1;

                    flipIndex = (y * width + x) * 3;
                    frameData[i] = taskByte[flipIndex];
                    frameData[i + 1] = taskByte[flipIndex + 1];
                    frameData[i + 2] = taskByte[flipIndex + 2];
                }
            }

            // 좌우 반전
            else if (flipOption == FlipOption.HorizontalFlip)
            {
                for (int i = 0; i < frameData.Length; i += 3)
                {
                    index = i / 3;

                    x = width - (index % width) - 1;
                    y = index / width;

                    flipIndex = (y * width + x) * 3;
                    frameData[i] = taskByte[flipIndex];
                    frameData[i + 1] = taskByte[flipIndex + 1];
                    frameData[i + 2] = taskByte[flipIndex + 2];

                }
            }

            // 상하 반전
            else if (flipOption == FlipOption.VerticalFlip)
            {
                for (int i = 0; i < frameData.Length; i += 3)
                {
                    index = i / 3;

                    x = index % width;
                    y = height - (index / width) - 1;

                    flipIndex = (y * width + x) * 3;
                    frameData[i] = taskByte[flipIndex];
                    frameData[i + 1] = taskByte[flipIndex + 1];
                    frameData[i + 2] = taskByte[flipIndex + 2];
                }
            }

            // 잘못된 반전 옵션이 들어온 경우
            else { return; }
        }
    }
}