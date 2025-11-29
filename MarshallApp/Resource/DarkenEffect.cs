using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MarshallApp.Resource
{
    public class DarkenEffect : ShaderEffect
    {
        private static readonly PixelShader Shader = new PixelShader()
        {
            UriSource = new Uri("/MarshallApp;component/Resource/Darken.ps", UriKind.Relative)
        };

        public DarkenEffect()
        {
            PixelShader = Shader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(DarknessProperty);
        }

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(
                "Input",
                typeof(DarkenEffect),
                0);

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public static readonly DependencyProperty DarknessProperty =
            DependencyProperty.Register(
                "Darkness",
                typeof(double),
                typeof(DarkenEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        public double Darkness
        {
            get => (double)GetValue(DarknessProperty);
            set => SetValue(DarknessProperty, value);
        }
    }
}