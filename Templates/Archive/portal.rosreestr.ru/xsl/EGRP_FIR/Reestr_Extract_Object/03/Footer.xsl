<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template name="Footer">
    <xsl:param name="Footer" />
    <p>
      <xsl:call-template name="Value">
        <xsl:with-param name="Node" select="$Footer/Content"/>
      </xsl:call-template>
    </p>
		<table border="0" cellspacing="0" cellpadding="0" width="100%">
			<tr>
				<td width="40%" class="ful">
          <xsl:call-template name="Value">
            <xsl:with-param name="Node" select="//Extract/eDocument/Sender/@Appointment"/>
          </xsl:call-template>
        </td>
				<td width="1%">&#160;</td>
				<td width="12%" class="ful">&#160;</td>
				<td width="1%">&#160;</td>
				<td class="ful">
          <xsl:call-template name="Value">
            <xsl:with-param name="Node" select="parent::ReestrExtract/DeclarAttribute/@Registrator"/>
          </xsl:call-template>
        </td>
			</tr>
			<tr>
				<td class="note">(должность уполномоченного должностного лица органа, осуществляющего государственную регистрацию прав)</td>
				<td>&#160;</td>
				<td class="note">(подпись, М.П.)</td>
				<td>&#160;</td>
				<td class="note">(фамилия, инициалы)</td>
			</tr>
		</table>
	</xsl:template>
</xsl:stylesheet>
